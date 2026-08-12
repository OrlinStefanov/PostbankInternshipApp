using System.Globalization;
using BinTool.Application.Abstractions;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Import;
using BinTool.Application.Services.BinImport;
using BinTool.Domain.Entities;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

public class BinCsvImportService : IBinCsvImportService
{
    private const string DateFormat = "yyyy-MM-dd";

    private const int StreamBufferSize = 64 * 1024;

    private static readonly string[] RequiredColumns = BinCsvTemplate.RequiredColumns;

    private static readonly CsvConfiguration CsvSettings = new(CultureInfo.InvariantCulture)
    {
        MissingFieldFound = null
    };

    private readonly IBinImportRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ICardSchemeDetector _schemeDetector;
    private readonly ILogger<BinCsvImportService> _logger;

    public BinCsvImportService(
        IBinImportRepository repository, ICurrentUser currentUser, IAuditLog audit,
        ICardSchemeDetector schemeDetector, ILogger<BinCsvImportService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _audit = audit;
        _schemeDetector = schemeDetector;
        _logger = logger;
    }

    public async Task<BinImportResult> ImportAsync(
        Stream csvStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ImportRunId"] = Guid.NewGuid(),
            ["ImportFile"] = fileName
        });

        ImportLog.Started(_logger, fileName, _currentUser.Name);

        var result = new BinImportResult { FileName = fileName };
        var lookups = new BinImportLookups(await _repository.LoadReferenceTablesAsync(cancellationToken));
        var history = StartHistory(fileName);

        using var reader = new StreamReader(
            csvStream, detectEncodingFromByteOrderMarks: true, bufferSize: StreamBufferSize);
        using var csv = new CsvReader(reader, CsvSettings);

        if (!TryReadHeader(csv, result, history, fileName, out var columns))
        {
            await FinalizeAsync(result, history, cancellationToken);
            return result;
        }

        var candidates = CollectCandidates(csv, columns, lookups, result, history);

        var now = DateTime.UtcNow;
        var classification = await ClassifyAsync(candidates, lookups, history, now, result, cancellationToken);

        await ApplyRevivalsAsync(classification.Revivals, now, cancellationToken);

        FinalizeCounts(result, history, classification);

        await PersistImportAsync(classification, lookups, cancellationToken);

        BuildConflictResponses(result, classification.StagedConflicts);
        result.ImportHistoryId = history.ImportHistoryId;

        ImportLog.Finished(_logger,
            fileName, history.Status, result.TotalRows, result.InsertedCount,
            result.UnchangedCount, result.RejectedCount, result.ConflictCount);

        if (result.ConflictCount > 0)
            ImportLog.ConflictsPending(_logger, fileName, result.ConflictCount);

        return result;
    }

    public async Task<ConflictResolutionResult> ResolveConflictsAsync(
        IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken = default)
    {
        var result = new ConflictResolutionResult();

        var decisions = IndexResolutions(resolutions);
        var ids = decisions.Keys.ToList();

        var conflicts = await _repository.GetPendingForUpdateAsync(ids, cancellationToken);
        var targets = await LoadTargetsAsync(conflicts, cancellationToken);

        var found = conflicts.Select(c => c.PendingBinConflictId).ToHashSet();
        result.NotFoundCount = ids.Count(id => !found.Contains(id));

        var now = DateTime.UtcNow;
        var updatesByHistory = new Dictionary<int, int>();
        var insertsByHistory = new Dictionary<int, int>();
        var insertedFromConflicts = new List<(BinRange Added, ResolvedRow Values)>();

        // Only needed to name the ids in the audit snapshots.
        var lookups = conflicts.Count > 0
            ? new BinImportLookups(await _repository.LoadReferenceTablesAsync(cancellationToken))
            : null;

        foreach (var conflict in conflicts)
        {
            ApplyResolution(
                conflict, decisions[conflict.PendingBinConflictId], targets, lookups!, now,
                result, updatesByHistory, insertsByHistory, insertedFromConflicts);
        }

        await ApplyHistoryDeltasAsync(updatesByHistory, insertsByHistory, cancellationToken);

        await PersistResolutionsAsync(insertedFromConflicts, lookups, cancellationToken);

        LogResolved(result);

        return result;
    }

    public async Task<List<BinConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
    {
        var lookups = new BinImportLookups(await _repository.LoadReferenceTablesAsync(cancellationToken));
        var pending = await _repository.ListPendingAsync(cancellationToken);

        var targetIds = pending
            .Where(c => c.TargetBinRangeId.HasValue)
            .Select(c => c.TargetBinRangeId!.Value)
            .Distinct()
            .ToList();
        var targets = (await _repository.GetRangesAsync(targetIds, cancellationToken))
            .ToDictionary(b => b.BinRangeId);

        var conflicts = new List<BinConflict>();
        foreach (var conflict in pending)
        {
            BinRange? target = null;
            if (conflict.TargetBinRangeId is int targetId)
                targets.TryGetValue(targetId, out target);

            var response = BuildPendingResponse(conflict, target, lookups);
            if (response is not null)
                conflicts.Add(response);
        }

        return conflicts;
    }

    private ImportHistory StartHistory(string fileName)
    {
        var history = new ImportHistory
        {
            FileName = fileName,
            ImportedByUserId = _currentUser.UserId,
            Status = "Success"
        };

        _repository.AddHistory(history);
        return history;
    }

    private bool TryReadHeader(
        CsvReader csv, BinImportResult result, ImportHistory history, string fileName,
        out CsvFieldIndexes columns)
    {
        columns = default;

        if (!csv.Read() || !csv.ReadHeader())
        {
            AddRejection(result, history, 0, "File is empty or has no header row", string.Empty);
            history.Status = "Failed";
            ImportLog.FileRejected(_logger, fileName, "the file is empty or has no header row");
            return false;
        }

        var header = csv.HeaderRecord ?? Array.Empty<string>();
        var missing = RequiredColumns.Where(c => Array.IndexOf(header, c) < 0).ToList();

        if (missing.Count > 0)
        {
            AddRejection(result, history, 0,
                $"Missing required column(s): {string.Join(", ", missing)}", string.Join(",", header));
            history.Status = "Failed";
            ImportLog.FileRejected(_logger,
                fileName, $"required column(s) missing: {string.Join(", ", missing)}");
            return false;
        }

        columns = CsvFieldIndexes.FromHeader(header);
        return true;
    }

    private List<ImportCandidate> CollectCandidates(
        CsvReader csv, CsvFieldIndexes columns, BinImportLookups lookups,
        BinImportResult result, ImportHistory history)
    {
        var seenPrefixes = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<ImportCandidate>();
        var rowNumber = 0;

        while (csv.Read())
        {
            rowNumber++;
            result.TotalRows++;

            var raw = csv.Parser.RawRecord.Trim();

            var prefix = csv.GetField(columns.Prefix)?.Trim();
            var cardScheme = csv.GetField(columns.CardScheme)?.Trim();
            var productType = csv.GetField(columns.ProductType)?.Trim();
            var fundingType = csv.GetField(columns.FundingType)?.Trim();
            var countryCode = csv.GetField(columns.CountryCode)?.Trim();
            var validFrom = csv.GetField(columns.ValidFrom)?.Trim();
            var validTo = columns.ValidTo >= 0 ? csv.GetField(columns.ValidTo)?.Trim() : null;

            if (!TryValidateRow(prefix, cardScheme, productType, fundingType,
                    countryCode, validFrom, validTo, out var row, out var reason))
            {
                AddRejection(result, history, rowNumber, reason, raw);
                continue;
            }

            if (!seenPrefixes.Add(row.Prefix))
            {
                AddRejection(result, history, rowNumber,
                    $"Duplicate prefix '{row.Prefix}' already appears earlier in the file", raw);
                continue;
            }

            if (!lookups.TryResolve(in row, out var resolved, out var lookupReason))
            {
                AddRejection(result, history, rowNumber, lookupReason, raw);
                continue;
            }

            candidates.Add(new ImportCandidate(rowNumber, raw, resolved));
        }

        return candidates;
    }

    private async Task<ClassifiedCandidates> ClassifyAsync(
        List<ImportCandidate> candidates, BinImportLookups lookups, ImportHistory history,
        DateTime now, BinImportResult result, CancellationToken cancellationToken)
    {
        var existingRows = await _repository.FindByPrefixesAsync(
            candidates.Select(c => c.Values.Prefix).ToList(), cancellationToken);

        var existing = existingRows.ToDictionary(r => r.Prefix, StringComparer.Ordinal);

        var classification = new ClassifiedCandidates(
            Inserted: new List<(BinRange, ResolvedRow)>(),
            Revivals: new Dictionary<int, ResolvedRow>(),
            RevivedFrom: new Dictionary<int, BinRangeSnapshot>(),
            StagedConflicts: new List<StagedConflict>());

        foreach (var candidate in candidates)
            ClassifyOne(candidate, existing, lookups, history, now, classification, result);

        return classification;
    }

    private void ClassifyOne(
        ImportCandidate candidate, Dictionary<string, BinRange> existing, BinImportLookups lookups,
        ImportHistory history, DateTime now, ClassifiedCandidates classification, BinImportResult result)
    {
        var v = candidate.Values;
        existing.TryGetValue(v.Prefix, out var current);

        List<BinFieldDiff>? liveDiffs = null;
        if (current is { IsDeleted: false })
        {
            liveDiffs = Diff(current, v, lookups);
            if (liveDiffs.Count == 0)
            {
                result.UnchangedCount++;
                return;
            }
        }

        var declaredScheme = lookups.CardSchemeName(v.CardSchemeId);
        var detected = _schemeDetector.Detect(v.Prefix);

        if (!_schemeDetector.Matches(detected, declaredScheme))
        {
            StageMismatch(candidate, current, liveDiffs, detected, declaredScheme, lookups, history, now, classification);
            return;
        }

        if (current is null)
        {
            AddInsert(v, now, classification);
            result.InsertedCount++;
            return;
        }

        if (current.IsDeleted)
        {
            classification.Revivals[current.BinRangeId] = v;
            classification.RevivedFrom[current.BinRangeId] = BinRangeSnapshotMapper.From(current, lookups);
            result.InsertedCount++;
            return;
        }

        // Live row with different values, scheme consistent: a plain value conflict.
        StageValueConflict(candidate, current, liveDiffs!, detected, lookups, history, now, classification);
    }

    private void StageMismatch(
        ImportCandidate candidate, BinRange? current, List<BinFieldDiff>? liveDiffs,
        DetectedScheme detected, string? declaredScheme, BinImportLookups lookups,
        ImportHistory history, DateTime now, ClassifiedCandidates classification)
    {
        var mismatch = PendingBinConflictMapper.From(candidate.Values, candidate.Raw, history, now, ConflictType.SchemeMismatch);
        mismatch.TargetBinRangeId = current?.BinRangeId;
        _repository.AddConflict(mismatch);

        var diffs = current is { IsDeleted: false } ? liveDiffs! : new List<BinFieldDiff>();
        classification.StagedConflicts.Add(new StagedConflict(
            mismatch, candidate.RowNumber, diffs,
            MismatchMessage(candidate.Values.Prefix, detected, declaredScheme),
            StoredSchemeAdvisory(current, detected, lookups)));
    }

    private void StageValueConflict(
        ImportCandidate candidate, BinRange current, List<BinFieldDiff> liveDiffs,
        DetectedScheme detected, BinImportLookups lookups, ImportHistory history, DateTime now,
        ClassifiedCandidates classification)
    {
        var conflict = PendingBinConflictMapper.From(candidate.Values, candidate.Raw, history, now, ConflictType.ValueConflict);
        conflict.TargetBinRangeId = current.BinRangeId;
        _repository.AddConflict(conflict);

        classification.StagedConflicts.Add(new StagedConflict(
            conflict, candidate.RowNumber, liveDiffs, null,
            StoredSchemeAdvisory(current, detected, lookups)));
    }

    private void AddInsert(ResolvedRow v, DateTime now, ClassifiedCandidates classification)
    {
        var added = BinRangeImportMapper.ForImport(v, now, _currentUser.Name);

        _repository.AddRange(added);
        classification.Inserted.Add((added, v));
    }

    private async Task ApplyRevivalsAsync(
        Dictionary<int, ResolvedRow> revivals, DateTime now, CancellationToken cancellationToken)
    {
        var tracked = await _repository.GetRangesForUpdateAsync(revivals.Keys, cancellationToken);

        foreach (var row in tracked)
        {
            BinRangeImportMapper.ApplyRevival(row, revivals[row.BinRangeId], now, _currentUser.Name);
        }
    }

    private static void FinalizeCounts(
        BinImportResult result, ImportHistory history, ClassifiedCandidates classification)
    {
        result.RejectedCount = result.Errors.Count;
        history.ImportedRows = result.InsertedCount;
        history.RejectedRows = result.RejectedCount;
        history.Status = result.RejectedCount == result.TotalRows && result.TotalRows > 0
            ? "Failed"
            : result.RejectedCount > 0 || classification.StagedConflicts.Count > 0
                ? "Partial"
                : "Success";
    }

    private async Task PersistImportAsync(
        ClassifiedCandidates classification, BinImportLookups lookups, CancellationToken cancellationToken)
    {
        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        RecordImportedRanges(classification.Inserted, classification.Revivals, classification.RevivedFrom, lookups);

        await _repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private void BuildConflictResponses(BinImportResult result, List<StagedConflict> staged)
    {
        foreach (var s in staged)
        {
            var detectedName = _schemeDetector.DisplayName(_schemeDetector.Detect(s.Entity.Prefix));
            result.Conflicts.Add(BinConflictMapper.FromStaged(s, detectedName));
        }

        result.ConflictCount = result.Conflicts.Count;
    }

    private static Dictionary<int, bool> IndexResolutions(IEnumerable<ConflictResolution> resolutions) =>
        resolutions
            .GroupBy(r => r.PendingBinConflictId)
            .ToDictionary(g => g.Key, g => g.Last().Update);

    private async Task<Dictionary<int, BinRange>> LoadTargetsAsync(
        IReadOnlyList<PendingBinConflict> conflicts, CancellationToken cancellationToken)
    {
        var targetIds = conflicts
            .Where(c => c.TargetBinRangeId.HasValue)
            .Select(c => c.TargetBinRangeId!.Value)
            .ToList();

        var rows = await _repository.GetRangesForUpdateAsync(targetIds, cancellationToken);
        return rows.ToDictionary(b => b.BinRangeId);
    }

    private void ApplyResolution(
        PendingBinConflict conflict, bool apply, Dictionary<int, BinRange> targets,
        BinImportLookups lookups, DateTime now, ConflictResolutionResult result,
        Dictionary<int, int> updatesByHistory, Dictionary<int, int> insertsByHistory,
        List<(BinRange Added, ResolvedRow Values)> insertedFromConflicts)
    {
        var values = new ResolvedRow(
            conflict.Prefix, conflict.CardSchemeId, conflict.ProductTypeId,
            conflict.FundingTypeId, conflict.CountryId, conflict.ValidFrom, conflict.ValidTo);

        if (apply && conflict.TargetBinRangeId is int targetId && targets.TryGetValue(targetId, out var target))
        {
            ApplyUpdate(target, conflict, lookups, now);
            result.UpdatedCount++;
            updatesByHistory[conflict.ImportHistoryId] =
                updatesByHistory.GetValueOrDefault(conflict.ImportHistoryId) + 1;
        }
        else if (apply && conflict.TargetBinRangeId is null)
        {
            var added = ApplyInsertFromConflict(conflict, now);
            insertedFromConflicts.Add((added, values));
            result.UpdatedCount++;
            insertsByHistory[conflict.ImportHistoryId] =
                insertsByHistory.GetValueOrDefault(conflict.ImportHistoryId) + 1;
        }
        else
        {
            conflict.Status = ConflictStatus.Discarded;
            result.DiscardedCount++;
        }

        if (conflict.Status != ConflictStatus.Discarded)
            conflict.Status = ConflictStatus.Applied;

        conflict.ResolvedAt = now;
        conflict.ResolvedBy = _currentUser.Name;
    }

    private void ApplyUpdate(BinRange target, PendingBinConflict conflict, BinImportLookups lookups, DateTime now)
    {
        var before = BinRangeSnapshotMapper.From(target, lookups);

        BinRangeImportMapper.ApplyResolved(target, conflict, now, _currentUser.Name);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, target.BinRangeId,
            before, BinRangeSnapshotMapper.From(target, lookups));
    }

    private BinRange ApplyInsertFromConflict(PendingBinConflict conflict, DateTime now)
    {
        var added = BinRangeImportMapper.ForImport(conflict, now, _currentUser.Name);

        _repository.AddRange(added);
        return added;
    }

    private async Task PersistResolutionsAsync(
        List<(BinRange Added, ResolvedRow Values)> insertedFromConflicts,
        BinImportLookups? lookups, CancellationToken cancellationToken)
    {
        // No inserts means no ids to backfill, so the single save is enough.
        if (insertedFromConflicts.Count == 0)
        {
            await _repository.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var (added, values) in insertedFromConflicts)
        {
            _audit.Record(AuditAction.Imported, AuditEntityTypes.BinRange, added.BinRangeId,
                null, BinRangeSnapshotMapper.From(values, lookups!, isDeleted: false));
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private BinConflict? BuildPendingResponse(
        PendingBinConflict conflict, BinRange? target, BinImportLookups lookups)
    {
        var incoming = new ResolvedRow(
            conflict.Prefix, conflict.CardSchemeId, conflict.ProductTypeId,
            conflict.FundingTypeId, conflict.CountryId, conflict.ValidFrom, conflict.ValidTo);

        var detected = _schemeDetector.Detect(conflict.Prefix);
        var detectedName = _schemeDetector.DisplayName(detected);
        var advisory = StoredSchemeAdvisory(target, detected, lookups);

        if (conflict.ConflictType == ConflictType.SchemeMismatch)
        {
            var diffs = target is not null ? Diff(target, incoming, lookups) : new();
            var message = MismatchMessage(conflict.Prefix, detected, lookups.CardSchemeName(conflict.CardSchemeId));
            return BinConflictMapper.ForPending(conflict, diffs, detectedName, advisory, message);
        }

        if (target is null)
            return null;

        return BinConflictMapper.ForPending(conflict, Diff(target, incoming, lookups), detectedName, advisory);
    }

    private void RecordImportedRanges(
        List<(BinRange Entity, ResolvedRow Values)> inserted,
        Dictionary<int, ResolvedRow> revivals,
        Dictionary<int, BinRangeSnapshot> revivedFrom,
        BinImportLookups lookups)
    {
        // Imported rather than Created: the trail says a range arrived in a file.
        foreach (var (entity, values) in inserted)
        {
            _audit.Record(AuditAction.Imported, AuditEntityTypes.BinRange, entity.BinRangeId,
                null, BinRangeSnapshotMapper.From(values, lookups, isDeleted: false));
        }

        foreach (var (binRangeId, values) in revivals)
        {
            _audit.Record(AuditAction.Imported, AuditEntityTypes.BinRange, binRangeId,
                revivedFrom[binRangeId], BinRangeSnapshotMapper.From(values, lookups, isDeleted: false));
        }
    }

    private void LogResolved(ConflictResolutionResult result) =>
        ImportLog.ConflictsResolved(_logger,
            result.UpdatedCount + result.DiscardedCount, _currentUser.Name,
            result.UpdatedCount, result.DiscardedCount);

    private async Task ApplyHistoryDeltasAsync(
        Dictionary<int, int> updatesByHistory,
        Dictionary<int, int> insertsByHistory,
        CancellationToken cancellationToken)
    {
        var historyIds = updatesByHistory.Keys.Union(insertsByHistory.Keys).ToList();
        if (historyIds.Count == 0)
            return;

        var histories = await _repository.GetHistoriesAsync(historyIds, cancellationToken);

        foreach (var history in histories)
        {
            history.UpdatedRows += updatesByHistory.GetValueOrDefault(history.ImportHistoryId);
            history.ImportedRows += insertsByHistory.GetValueOrDefault(history.ImportHistoryId);
        }
    }

    private static bool TryValidateRow(
        string? prefix, string? cardScheme, string? productType, string? fundingType,
        string? countryCode, string? validFrom, string? validTo,
        out ParsedRow row, out string reason)
    {
        row = default;
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length is < 6 or > 8 || !IsAllDigits(prefix))
        {
            reason = "Prefix must be 6-8 digits";
            return false;
        }

        if (string.IsNullOrWhiteSpace(cardScheme)) { reason = "CardScheme is required"; return false; }
        if (string.IsNullOrWhiteSpace(productType)) { reason = "ProductType is required"; return false; }
        if (string.IsNullOrWhiteSpace(fundingType)) { reason = "FundingType is required"; return false; }

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2 || !IsAllLetters(countryCode))
        {
            reason = "CountryCode must be a 2-letter ISO code";
            return false;
        }

        if (!DateTime.TryParseExact(validFrom, DateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var from))
        {
            reason = $"ValidFrom must be a valid date ({DateFormat})";
            return false;
        }

        DateTime? to = null;

        if (!string.IsNullOrWhiteSpace(validTo))
        {
            if (!DateTime.TryParseExact(validTo, DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedTo))
            {
                reason = $"ValidTo must be a valid date ({DateFormat})";
                return false;
            }

            if (parsedTo <= from)
            {
                reason = "ValidTo must be after ValidFrom";
                return false;
            }

            to = parsedTo;
        }

        row = new ParsedRow(prefix, cardScheme, productType, fundingType, countryCode, from, to);
        return true;
    }

    private static bool IsAllDigits(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsDigit(c)) return false;
        }

        return true;
    }

    private static bool IsAllLetters(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsLetter(c)) return false;
        }

        return true;
    }

    private static List<BinFieldDiff> Diff(BinRange current, ResolvedRow incoming, BinImportLookups lookups)
    {
        var diffs = new List<BinFieldDiff>();

        if (current.CardSchemeId != incoming.CardSchemeId)
        {
            diffs.Add(new BinFieldDiff
            {
                Field = "CardScheme",
                OldValue = lookups.CardSchemeName(current.CardSchemeId),
                NewValue = lookups.CardSchemeName(incoming.CardSchemeId)
            });
        }

        if (current.ProductTypeId != incoming.ProductTypeId)
        {
            diffs.Add(new BinFieldDiff
            {
                Field = "ProductType",
                OldValue = lookups.ProductTypeName(current.ProductTypeId),
                NewValue = lookups.ProductTypeName(incoming.ProductTypeId)
            });
        }

        if (current.FundingTypeId != incoming.FundingTypeId)
        {
            diffs.Add(new BinFieldDiff
            {
                Field = "FundingType",
                OldValue = lookups.FundingTypeName(current.FundingTypeId),
                NewValue = lookups.FundingTypeName(incoming.FundingTypeId)
            });
        }

        if (current.CountryId != incoming.CountryId)
        {
            diffs.Add(new BinFieldDiff
            {
                Field = "CountryCode",
                OldValue = lookups.CountryCode(current.CountryId),
                NewValue = lookups.CountryCode(incoming.CountryId)
            });
        }

        if (current.ValidFrom != incoming.ValidFrom)
        {
            diffs.Add(new BinFieldDiff
            {
                Field = "ValidFrom",
                OldValue = current.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture),
                NewValue = incoming.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture)
            });
        }

        if (current.ValidTo != incoming.ValidTo)
        {
            diffs.Add(new BinFieldDiff
            {
                Field = "ValidTo",
                OldValue = current.ValidTo?.ToString(DateFormat, CultureInfo.InvariantCulture),
                NewValue = incoming.ValidTo?.ToString(DateFormat, CultureInfo.InvariantCulture)
            });
        }

        return diffs;
    }

    private string MismatchMessage(string prefix, DetectedScheme detected, string? declared)
    {
        var declaredName = string.IsNullOrWhiteSpace(declared) ? "an unknown scheme" : declared;
        var detectedName = _schemeDetector.DisplayName(detected);

        return detectedName is null
            ? $"Prefix {prefix} does not match any known card-scheme range, but the file declares {declaredName}."
            : $"Prefix {prefix} is a {detectedName} range, but the file declares {declaredName}.";
    }

    private string? StoredSchemeAdvisory(BinRange? target, DetectedScheme detected, BinImportLookups lookups)
    {
        if (target is null) return null;

        var detectedName = _schemeDetector.DisplayName(detected);
        if (detectedName is null) return null;

        var storedName = lookups.CardSchemeName(target.CardSchemeId);
        if (_schemeDetector.Matches(detected, storedName)) return null;

        var storedLabel = string.IsNullOrWhiteSpace(storedName) ? "an unknown scheme" : storedName;
        return $"The stored row is on {storedLabel}, but prefix {target.Prefix} is a {detectedName} range.";
    }

    private static void AddRejection(
        BinImportResult result, ImportHistory history, int rowNumber, string reason, string raw)
    {
        result.Errors.Add(new BinImportError { RowNumber = rowNumber, Reason = reason, RawData = raw });

        history.RejectedRows_Navigation.Add(new RejectedImportRow
        {
            RowNumber = rowNumber,
            Reason = reason,
            RawData = raw
        });
    }

    private async Task FinalizeAsync(
        BinImportResult result, ImportHistory history, CancellationToken cancellationToken)
    {
        result.RejectedCount = result.Errors.Count;
        history.RejectedRows = result.RejectedCount;

        await _repository.SaveChangesAsync(cancellationToken);

        result.ImportHistoryId = history.ImportHistoryId;
    }
}
