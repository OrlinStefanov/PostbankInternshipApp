using System.Globalization;
using BinTool.Application.Abstractions;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Import;
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
        // Everything logged for the rest of this call carries the run id and the file name. The run
        // id is generated here rather than taken from the history row, which has no id until the
        // first save - and the failure paths above that save need to be traceable too.
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ImportRunId"] = Guid.NewGuid(),
            ["ImportFile"] = fileName
        });

        ImportLog.Started(_logger, fileName, _currentUser.Name);

        var result = new BinImportResult { FileName = fileName };

        // Load the lookup tables once so every row resolves against in-memory maps.
        var lookups = new Lookups(await _repository.LoadReferenceTablesAsync(cancellationToken));

        var history = new ImportHistory
        {
            FileName = fileName,
            ImportedByUserId = _currentUser.UserId,
            Status = "Success"
        };

        _repository.AddHistory(history);

        using var reader = new StreamReader(
            csvStream, detectEncodingFromByteOrderMarks: true, bufferSize: StreamBufferSize);
        using var csv = new CsvReader(reader, CsvSettings);

        if (!csv.Read() || !csv.ReadHeader())
        {
            AddRejection(result, history, 0, "File is empty or has no header row", string.Empty);

            history.Status = "Failed";

            ImportLog.FileRejected(_logger, fileName, "the file is empty or has no header row");

            await FinalizeAsync(result, history, cancellationToken);

            return result;
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

            await FinalizeAsync(result, history, cancellationToken);

            return result;
        }

        // Resolve column positions once. Reading fields by index in the loop avoids a
        // name lookup per field per row.
        var columns = FieldIndexes.FromHeader(header);

        // First pass: parse, validate structure, resolve lookups, and collect the
        // rows we might persist. We defer the existing-row lookup so it can be
        // batched instead of hitting the database per row.
        var seenPrefixes = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<Candidate>();
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

            candidates.Add(new Candidate(rowNumber, raw, resolved));
        }

        // Second pass: reconcile the resolved rows against the existing BIN ranges.
        // Soft-deleted rows are included deliberately: the unique index on Prefix spans
        // them, so inserting alongside one would violate the constraint.
        var existingRows = await _repository.FindByPrefixesAsync(
            candidates.Select(c => c.Values.Prefix).ToList(), cancellationToken);

        var existing = existingRows.ToDictionary(r => r.Prefix, StringComparer.Ordinal);

        var now = DateTime.UtcNow;
        var stagedConflicts = new List<(PendingBinConflict Entity, int RowNumber, List<BinFieldDiff> Diffs, string? Message, string? Advisory)>();
        var revivals = new Dictionary<int, ResolvedRow>();

        // Rows that changed live data, kept so each can be audited once its id exists.
        var inserted = new List<(BinRange Entity, ResolvedRow Values)>();
        var revivedFrom = new Dictionary<int, BinRangeSnapshot>();

        foreach (var candidate in candidates)
        {
            var v = candidate.Values;
            existing.TryGetValue(v.Prefix, out var current);

            // A row identical to the live record changes nothing, and running a scheme
            // check over data that is already stored would only raise noise - so settle
            // the unchanged case first and move on.
            List<BinFieldDiff>? liveDiffs = null;
            if (current is { IsDeleted: false })
            {
                liveDiffs = Diff(current, v, lookups);
                if (liveDiffs.Count == 0)
                {
                    result.UnchangedCount++;
                    continue;
                }
            }

            // A declared scheme that contradicts the network the prefix belongs to (or a
            // prefix in no known range at all) is held for review rather than trusted -
            // whether the row would otherwise insert, revive, or update an existing range.
            var declaredScheme = lookups.CardSchemeName(v.CardSchemeId);

            var detected = _schemeDetector.Detect(v.Prefix);

            if (!_schemeDetector.Matches(detected, declaredScheme))
            {
                var mismatch = NewConflict(v, candidate.Raw, history, now, ConflictType.SchemeMismatch);

                mismatch.TargetBinRangeId = current?.BinRangeId;

                _repository.AddConflict(mismatch);

                // Only a live target has an existing record to diff against; an insert or
                // a revival is explained by the message alone.
                var diffs = current is { IsDeleted: false } ? liveDiffs! : new List<BinFieldDiff>();
                stagedConflicts.Add((mismatch, candidate.RowNumber, diffs,
                    MismatchMessage(v.Prefix, detected, declaredScheme),
                    StoredSchemeAdvisory(current, detected, lookups)));
                continue;
            }

            if (current is null)
            {
                var added = new BinRange
                {
                    Prefix = v.Prefix,
                    PrefixLength = v.Prefix.Length,
                    CardSchemeId = v.CardSchemeId,
                    ProductTypeId = v.ProductTypeId,
                    FundingTypeId = v.FundingTypeId,
                    CountryId = v.CountryId,
                    ValidFrom = v.ValidFrom,
                    ValidTo = v.ValidTo,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = _currentUser.Name,
                    UpdatedBy = _currentUser.Name
                };

                _repository.AddRange(added);

                inserted.Add((added, v));
                result.InsertedCount++;

                continue;
            }

            // A soft-deleted range is not live data, so there is nothing for the user to arbitrate:
            // revive the existing row in place with the imported values, keeping its identity and
            // audit trail.
            if (current.IsDeleted)
            {
                revivals[current.BinRangeId] = v;
                revivedFrom[current.BinRangeId] = Snapshot(current, lookups);
                result.InsertedCount++;
                continue;
            }

            // A live row with different values, scheme consistent: a plain value conflict.
            var conflict = NewConflict(v, candidate.Raw, history, now, ConflictType.ValueConflict);
            conflict.TargetBinRangeId = current.BinRangeId;
            _repository.AddConflict(conflict);

            stagedConflicts.Add((conflict, candidate.RowNumber, liveDiffs!, null,
                StoredSchemeAdvisory(current, detected, lookups)));
        }

        // Revived rows are re-read with tracking so the change tracker owns the instance
        // (attaching the no-tracking copy would clash with anything already tracked).
        var tracked = await _repository.GetRangesForUpdateAsync(revivals.Keys, cancellationToken);

        foreach (var row in tracked)
        {
            var v = revivals[row.BinRangeId];

            row.PrefixLength = v.Prefix.Length;
            row.CardSchemeId = v.CardSchemeId;
            row.ProductTypeId = v.ProductTypeId;
            row.FundingTypeId = v.FundingTypeId;
            row.CountryId = v.CountryId;
            row.ValidFrom = v.ValidFrom;
            row.ValidTo = v.ValidTo;
            row.IsDeleted = false;
            row.DeletedAt = null;
            row.DeletedBy = null;
            row.UpdatedAt = now;
            row.UpdatedBy = _currentUser.Name;
        }

        result.RejectedCount = result.Errors.Count;
        history.ImportedRows = result.InsertedCount;
        history.RejectedRows = result.RejectedCount;
        history.Status = result.RejectedCount == result.TotalRows && result.TotalRows > 0
            ? "Failed"
            : result.RejectedCount > 0 || stagedConflicts.Count > 0
                ? "Partial"
                : "Success";

        // Inserted ranges have no id until they are saved and an audit entry has to carry one, so
        // the trail is written in a second save with a transaction holding the two together - a
        // quietly incomplete trail is the one failure an audit trail cannot have.
        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        RecordImportedRanges(inserted, revivals, revivedFrom, lookups);

        await _repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Ids are generated now, so build the response conflicts.
        foreach (var (entity, stagedRowNumber, diffs, message, advisory) in stagedConflicts)
        {
            result.Conflicts.Add(new BinConflict
            {
                PendingBinConflictId = entity.PendingBinConflictId,
                RowNumber = stagedRowNumber,
                Prefix = entity.Prefix,
                ConflictType = entity.ConflictType.ToString(),
                Message = message,
                Differences = diffs,
                DetectedScheme = _schemeDetector.DisplayName(_schemeDetector.Detect(entity.Prefix)),
                SchemeAdvisory = advisory
            });
        }

        result.ConflictCount = result.Conflicts.Count;
        result.ImportHistoryId = history.ImportHistoryId;

        ImportLog.Finished(_logger,
            fileName, history.Status, result.TotalRows, result.InsertedCount,
            result.UnchangedCount, result.RejectedCount, result.ConflictCount);

        // Separate from the summary and at Warning, because a pending conflict is work the
        // import could not finish on its own - the run "succeeded" but the data is not yet
        // what the file said it should be.
        if (result.ConflictCount > 0)
        {
            ImportLog.ConflictsPending(_logger, fileName, result.ConflictCount);
        }

        return result;
    }

    public async Task<ConflictResolutionResult> ResolveConflictsAsync(
        IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken = default)
    {
        var result = new ConflictResolutionResult();

        var decisions = resolutions
            .GroupBy(r => r.PendingBinConflictId)
            .ToDictionary(g => g.Key, g => g.Last().Update);

        var ids = decisions.Keys.ToList();

        var conflicts = await _repository.GetPendingForUpdateAsync(ids, cancellationToken);

        // Value conflicts and scheme mismatches on an existing prefix carry a target to
        // overwrite; a scheme mismatch on a new prefix has none and inserts on apply.
        var targetIds = conflicts
            .Where(c => c.TargetBinRangeId.HasValue)
            .Select(c => c.TargetBinRangeId!.Value)
            .ToList();
        var targets = (await _repository.GetRangesForUpdateAsync(targetIds, cancellationToken))
            .ToDictionary(b => b.BinRangeId);

        var found = conflicts.Select(c => c.PendingBinConflictId).ToHashSet();
        result.NotFoundCount = ids.Count(id => !found.Contains(id));

        var now = DateTime.UtcNow;

        // Count applied conflicts back against the originating import, so its ImportHistory totals
        // reflect what its conflicts ultimately produced once they are resolved.
        var updatesByHistory = new Dictionary<int, int>();
        var insertsByHistory = new Dictionary<int, int>();

        // New ranges have no id until saved, and an audit entry needs one, so they are
        // audited in a second save inside a transaction (as the import itself does).
        var insertedFromConflicts = new List<(BinRange Added, ResolvedRow Values)>();

        // Only needed to name the ids in the audit snapshots.
        var lookups = conflicts.Count > 0
            ? new Lookups(await _repository.LoadReferenceTablesAsync(cancellationToken))
            : null;

        foreach (var conflict in conflicts)
        {
            var apply = decisions[conflict.PendingBinConflictId];
            var values = new ResolvedRow(
                conflict.Prefix, conflict.CardSchemeId, conflict.ProductTypeId,
                conflict.FundingTypeId, conflict.CountryId, conflict.ValidFrom, conflict.ValidTo);

            if (apply && conflict.TargetBinRangeId is int targetId
                && targets.TryGetValue(targetId, out var target))
            {
                // Applying overwrites the existing row (reviving it if it had been
                // soft-deleted), so the values it replaces are captured first.
                var before = Snapshot(target, lookups!);

                target.CardSchemeId = conflict.CardSchemeId;
                target.ProductTypeId = conflict.ProductTypeId;
                target.FundingTypeId = conflict.FundingTypeId;
                target.CountryId = conflict.CountryId;
                target.PrefixLength = conflict.PrefixLength;
                target.ValidFrom = conflict.ValidFrom;
                target.ValidTo = conflict.ValidTo;
                target.IsDeleted = false;
                target.DeletedAt = null;
                target.DeletedBy = null;
                target.UpdatedAt = now;
                target.UpdatedBy = _currentUser.Name;

                _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, target.BinRangeId,
                    before, Snapshot(target, lookups!));

                conflict.Status = ConflictStatus.Applied;
                result.UpdatedCount++;
                updatesByHistory[conflict.ImportHistoryId] =
                    updatesByHistory.GetValueOrDefault(conflict.ImportHistoryId) + 1;
            }
            else if (apply && conflict.TargetBinRangeId is null)
            {
                // A scheme mismatch on a brand-new prefix: applying trusts the file and
                // inserts the range as declared. Audited once it has an id, below.
                var added = new BinRange
                {
                    Prefix = conflict.Prefix,
                    PrefixLength = conflict.PrefixLength,
                    CardSchemeId = conflict.CardSchemeId,
                    ProductTypeId = conflict.ProductTypeId,
                    FundingTypeId = conflict.FundingTypeId,
                    CountryId = conflict.CountryId,
                    ValidFrom = conflict.ValidFrom,
                    ValidTo = conflict.ValidTo,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = _currentUser.Name,
                    UpdatedBy = _currentUser.Name
                };

                _repository.AddRange(added);
                insertedFromConflicts.Add((added, values));

                conflict.Status = ConflictStatus.Applied;
                result.UpdatedCount++;
                insertsByHistory[conflict.ImportHistoryId] =
                    insertsByHistory.GetValueOrDefault(conflict.ImportHistoryId) + 1;
            }
            else
            {
                // Discarding changes no BIN range, so there is nothing to snapshot. Who
                // decided, and when, is recorded on the conflict itself just below. A
                // stale update whose target row has vanished lands here too.
                conflict.Status = ConflictStatus.Discarded;
                result.DiscardedCount++;
            }

            conflict.ResolvedAt = now;
            conflict.ResolvedBy = _currentUser.Name;
        }

        await ApplyHistoryDeltasAsync(updatesByHistory, insertsByHistory, cancellationToken);

        // No inserts means no ids to backfill, so the single save is enough.
        if (insertedFromConflicts.Count == 0)
        {
            await _repository.SaveChangesAsync(cancellationToken);

            LogResolved(result);

            return result;
        }

        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var (added, values) in insertedFromConflicts)
        {
            _audit.Record(AuditAction.Imported, AuditEntityTypes.BinRange, added.BinRangeId,
                null, Snapshot(values, lookups!, isDeleted: false));
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogResolved(result);

        return result;
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

    public async Task<List<BinConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
    {
        var lookups = new Lookups(await _repository.LoadReferenceTablesAsync(cancellationToken));

        // Read-only projection for display, so nothing here needs change tracking.
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

            var incoming = new ResolvedRow(
                conflict.Prefix, conflict.CardSchemeId, conflict.ProductTypeId,
                conflict.FundingTypeId, conflict.CountryId, conflict.ValidFrom, conflict.ValidTo);

            var detected = _schemeDetector.Detect(conflict.Prefix);
            var detectedName = _schemeDetector.DisplayName(detected);
            var advisory = StoredSchemeAdvisory(target, detected, lookups);

            if (conflict.ConflictType == ConflictType.SchemeMismatch)
            {
                // A scheme mismatch stands on its own - a new-prefix mismatch never has a
                // target, so a missing one is not staleness. Diff only when there is an
                // existing row it would overwrite.
                conflicts.Add(new BinConflict
                {
                    PendingBinConflictId = conflict.PendingBinConflictId,
                    RowNumber = 0, // not meaningful outside the originating file
                    Prefix = conflict.Prefix,
                    ConflictType = conflict.ConflictType.ToString(),
                    Message = MismatchMessage(conflict.Prefix, detected,
                        lookups.CardSchemeName(conflict.CardSchemeId)),
                    Differences = target is not null ? Diff(target, incoming, lookups) : new(),
                    DetectedScheme = detectedName,
                    SchemeAdvisory = advisory
                });
                continue;
            }

            // A value conflict only exists against a live row; if the target is gone the
            // conflict is stale, so skip it.
            if (target is null)
                continue;

            conflicts.Add(new BinConflict
            {
                PendingBinConflictId = conflict.PendingBinConflictId,
                RowNumber = 0, // not meaningful outside the originating file
                Prefix = conflict.Prefix,
                ConflictType = conflict.ConflictType.ToString(),
                Differences = Diff(target, incoming, lookups),
                DetectedScheme = detectedName,
                SchemeAdvisory = advisory
            });
        }

        return conflicts;
    }

    // Only the rows that touched live BIN data are recorded. A rejected row changed nothing and is
    // already kept with its reason on the import history; a staged conflict is audited if and when
    // someone applies it.
    private void RecordImportedRanges(
        List<(BinRange Entity, ResolvedRow Values)> inserted,
        Dictionary<int, ResolvedRow> revivals,
        Dictionary<int, BinRangeSnapshot> revivedFrom,
        Lookups lookups)
    {
        // Imported rather than Created: the trail should say a range arrived in a file
        // rather than being typed in by hand.
        foreach (var (entity, values) in inserted)
        {
            _audit.Record(AuditAction.Imported, AuditEntityTypes.BinRange, entity.BinRangeId,
                null, Snapshot(values, lookups, isDeleted: false));
        }

        foreach (var (binRangeId, values) in revivals)
        {
            _audit.Record(AuditAction.Imported, AuditEntityTypes.BinRange, binRangeId,
                revivedFrom[binRangeId], Snapshot(values, lookups, isDeleted: false));
        }
    }

    // Describes a stored range for the audit trail, resolving its lookup ids through the snapshot
    // the import already loaded rather than going back to the database per row.
    private static BinRangeSnapshot Snapshot(BinRange range, Lookups lookups) => new(
        range.Prefix,
        lookups.CardSchemeName(range.CardSchemeId),
        lookups.ProductTypeName(range.ProductTypeId),
        lookups.FundingTypeName(range.FundingTypeId),
        lookups.CountryCode(range.CountryId),
        BinRangeSnapshot.Date(range.ValidFrom),
        range.ValidTo is null ? null : BinRangeSnapshot.Date(range.ValidTo.Value),
        range.IsDeleted);

    private static BinRangeSnapshot Snapshot(ResolvedRow row, Lookups lookups, bool isDeleted) => new(
        row.Prefix,
        lookups.CardSchemeName(row.CardSchemeId),
        lookups.ProductTypeName(row.ProductTypeId),
        lookups.FundingTypeName(row.FundingTypeId),
        lookups.CountryCode(row.CountryId),
        BinRangeSnapshot.Date(row.ValidFrom),
        row.ValidTo is null ? null : BinRangeSnapshot.Date(row.ValidTo.Value),
        isDeleted);

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

    // Compares an incoming row against the existing record, returning a diff entry for every field
    // that differs (formatted for display). An empty list means the records are identical and the
    // row can be skipped.
    private static List<BinFieldDiff> Diff(BinRange current, ResolvedRow incoming, Lookups lookups)
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

    // Builds a staged conflict from an import row, linked to its originating import. The caller
    // sets TargetBinRangeId (an existing row to overwrite, or null to insert on apply).
    private static PendingBinConflict NewConflict(
        ResolvedRow v, string raw, ImportHistory history, DateTime now, ConflictType type) => new()
        {
            ConflictType = type,
            Prefix = v.Prefix,
            PrefixLength = v.Prefix.Length,
            CardSchemeId = v.CardSchemeId,
            ProductTypeId = v.ProductTypeId,
            FundingTypeId = v.FundingTypeId,
            CountryId = v.CountryId,
            ValidFrom = v.ValidFrom,
            ValidTo = v.ValidTo,
            RawData = raw,
            Status = ConflictStatus.Pending,
            CreatedAt = now,
            ImportHistory = history
        };

    // A one-line explanation of why a row's declared scheme was not trusted: either it names a
    // different network than the prefix belongs to, or the prefix matches no known network at all.
    private string MismatchMessage(string prefix, DetectedScheme detected, string? declared)
    {
        var declaredName = string.IsNullOrWhiteSpace(declared) ? "an unknown scheme" : declared;
        var detectedName = _schemeDetector.DisplayName(detected);

        return detectedName is null
            ? $"Prefix {prefix} does not match any known card-scheme range, but the file declares {declaredName}."
            : $"Prefix {prefix} is a {detectedName} range, but the file declares {declaredName}.";
    }

    // Non-null when the target row already in the database holds a scheme the detector disagrees
    // with - a reviewer needs telling the stored row is wrong for its prefix even when the incoming
    // row is not changing it.
    private string? StoredSchemeAdvisory(BinRange? target, DetectedScheme detected, Lookups lookups)
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

    private sealed record Candidate(int RowNumber, string Raw, ResolvedRow Values);

    private readonly struct FieldIndexes
    {
        public int Prefix { get; private init; }
        public int CardScheme { get; private init; }
        public int ProductType { get; private init; }
        public int FundingType { get; private init; }
        public int CountryCode { get; private init; }
        public int ValidFrom { get; private init; }
        public int ValidTo { get; private init; }

        public static FieldIndexes FromHeader(string[] header) => new()
        {
            Prefix = Array.IndexOf(header, "Prefix"),
            CardScheme = Array.IndexOf(header, "CardScheme"),
            ProductType = Array.IndexOf(header, "ProductType"),
            FundingType = Array.IndexOf(header, "FundingType"),
            CountryCode = Array.IndexOf(header, "CountryCode"),
            ValidFrom = Array.IndexOf(header, "ValidFrom"),
            ValidTo = Array.IndexOf(header, "ValidTo")
        };
    }

    // A structurally-valid row before its lookup names are resolved to ids. A struct so the parse
    // loop does not allocate one object per row.
    private readonly record struct ParsedRow(
        string Prefix, string CardScheme, string ProductType, string FundingType,
        string CountryCode, DateTime ValidFrom, DateTime? ValidTo);

    private sealed record ResolvedRow(
        string Prefix, int CardSchemeId, int ProductTypeId, int FundingTypeId, int CountryId,
        DateTime ValidFrom, DateTime? ValidTo);

    // The reference tables as loaded, plus the resolution rule: an incoming row's four names either
    // all resolve to live ids or the row is rejected naming the first one that did not. Loading is
    // storage and lives in the repository; deciding is not.
    private sealed class Lookups
    {
        private readonly ReferenceTables _tables;

        public Lookups(ReferenceTables tables)
        {
            _tables = tables;
        }

        public bool TryResolve(in ParsedRow row, out ResolvedRow resolved, out string reason)
        {
            resolved = default!;
            reason = string.Empty;

            if (!_tables.CardSchemeIds.TryGetValue(row.CardScheme, out var cardSchemeId))
            {
                reason = $"CardScheme '{row.CardScheme}' does not exist";
                return false;
            }

            if (!_tables.ProductTypeIds.TryGetValue(row.ProductType, out var productTypeId))
            {
                reason = $"ProductType '{row.ProductType}' does not exist";
                return false;
            }

            if (!_tables.FundingTypeIds.TryGetValue(row.FundingType, out var fundingTypeId))
            {
                reason = $"FundingType '{row.FundingType}' does not exist";
                return false;
            }

            if (!_tables.CountryIds.TryGetValue(row.CountryCode, out var countryId))
            {
                reason = $"CountryCode '{row.CountryCode}' does not exist";
                return false;
            }

            resolved = new ResolvedRow(
                row.Prefix, cardSchemeId, productTypeId, fundingTypeId, countryId,
                row.ValidFrom, row.ValidTo);

            return true;
        }

        public string? CardSchemeName(int id) => _tables.CardSchemeNames.GetValueOrDefault(id);
        public string? ProductTypeName(int id) => _tables.ProductTypeNames.GetValueOrDefault(id);
        public string? FundingTypeName(int id) => _tables.FundingTypeNames.GetValueOrDefault(id);
        public string? CountryCode(int id) => _tables.CountryCodes.GetValueOrDefault(id);
    }
}
