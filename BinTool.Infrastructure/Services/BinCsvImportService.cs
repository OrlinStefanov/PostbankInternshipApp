using System.Globalization;
using BinTool.Core.Entities;
using BinTool.Core.Models.Audit;
using BinTool.Core.Models.Import;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class BinCsvImportService : IBinCsvImportService
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Read buffer for the CSV stream. Larger than the 1 KB default so bulk files
    /// need far fewer underlying reads.
    /// </summary>
    private const int StreamBufferSize = 64 * 1024;

    /// <summary>
    /// Existing prefixes are looked up in batches so a large file does not build a
    /// single enormous IN (...) clause, which is slow to plan and can exceed the
    /// provider's parameter limit (SQLite caps host parameters per statement).
    /// </summary>
    private const int PrefixLookupBatchSize = 500;

    private static readonly string[] RequiredColumns =
    {
        "Prefix", "CardScheme", "ProductType", "FundingType", "CountryCode", "ValidFrom"
    };

    /// <summary>
    /// A short row must not abort the whole import: with no MissingFieldFound handler
    /// CsvHelper returns null for absent fields, which validation then rejects per row.
    /// </summary>
    private static readonly CsvConfiguration CsvSettings = new(CultureInfo.InvariantCulture)
    {
        MissingFieldFound = null
    };

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public BinCsvImportService(AppDbContext db, ICurrentUser currentUser, IAuditLog audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<BinImportResult> ImportAsync(
        Stream csvStream, string fileName, CancellationToken cancellationToken = default)
    {
        var result = new BinImportResult { FileName = fileName };

        // Load the lookup tables once so every row resolves against in-memory maps.
        var lookups = await Lookups.LoadAsync(_db, cancellationToken);

        var history = new ImportHistory
        {
            FileName = fileName,
            ImportedByUserId = _currentUser.UserId,
            Status = "Success"
        };

        _db.ImportHistories.Add(history);

        using var reader = new StreamReader(
            csvStream, detectEncodingFromByteOrderMarks: true, bufferSize: StreamBufferSize);
        using var csv = new CsvReader(reader, CsvSettings);

        if (!csv.Read() || !csv.ReadHeader())
        {
            AddRejection(result, history, 0, "File is empty or has no header row", string.Empty);
            history.Status = "Failed";
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
        var existing = new Dictionary<string, BinRange>(candidates.Count, StringComparer.Ordinal);
        foreach (var batch in candidates.Select(c => c.Values.Prefix).Chunk(PrefixLookupBatchSize))
        {
            // Read-only for the common compare path; the rare revived row is attached
            // explicitly below.
            var rows = await _db.BinRanges
                .AsNoTracking()
                .Where(b => batch.Contains(b.Prefix))
                .ToListAsync(cancellationToken);

            foreach (var existingRow in rows)
                existing[existingRow.Prefix] = existingRow;
        }

        var now = DateTime.UtcNow;
        var stagedConflicts = new List<(PendingBinConflict Entity, int RowNumber, List<BinFieldDiff> Diffs)>();
        var revivals = new Dictionary<int, ResolvedRow>();

        // Rows that changed live data, kept so each can be audited once its id exists.
        var inserted = new List<(BinRange Entity, ResolvedRow Values)>();
        var revivedFrom = new Dictionary<int, BinRangeSnapshot>();

        foreach (var candidate in candidates)
        {
            var v = candidate.Values;

            if (!existing.TryGetValue(v.Prefix, out var current))
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

                _db.BinRanges.Add(added);
                inserted.Add((added, v));
                result.InsertedCount++;
                continue;
            }

            // A soft-deleted range is not live data, so there is nothing for the user to
            // arbitrate: revive the existing row in place with the imported values. The
            // row keeps its identity and audit trail, and the prefix stays unique.
            // Applied further down, against tracked instances.
            if (current.IsDeleted)
            {
                revivals[current.BinRangeId] = v;
                revivedFrom[current.BinRangeId] = Snapshot(current, lookups);
                result.InsertedCount++;
                continue;
            }

            var diffs = Diff(current, v, lookups);
            if (diffs.Count == 0)
            {
                result.UnchangedCount++;
                continue;
            }

            var conflict = new PendingBinConflict
            {
                TargetBinRangeId = current.BinRangeId,
                Prefix = v.Prefix,
                PrefixLength = v.Prefix.Length,
                CardSchemeId = v.CardSchemeId,
                ProductTypeId = v.ProductTypeId,
                FundingTypeId = v.FundingTypeId,
                CountryId = v.CountryId,
                ValidFrom = v.ValidFrom,
                ValidTo = v.ValidTo,
                RawData = candidate.Raw,
                Status = ConflictStatus.Pending,
                CreatedAt = now
            };
            conflict.ImportHistory = history;
            _db.Set<PendingBinConflict>().Add(conflict);

            stagedConflicts.Add((conflict, candidate.RowNumber, diffs));
        }

        // Revived rows are re-read with tracking so the change tracker owns the instance
        // (attaching the no-tracking copy would clash with anything already tracked).
        foreach (var batch in revivals.Keys.Chunk(PrefixLookupBatchSize))
        {
            var rows = await _db.BinRanges
                .Where(b => batch.Contains(b.BinRangeId))
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
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
        }

        result.RejectedCount = result.Errors.Count;
        history.ImportedRows = result.InsertedCount;
        history.RejectedRows = result.RejectedCount;
        history.Status = result.RejectedCount == result.TotalRows && result.TotalRows > 0
            ? "Failed"
            : result.RejectedCount > 0 || stagedConflicts.Count > 0
                ? "Partial"
                : "Success";

        // Inserted ranges have no id until they are saved, and an audit entry has to carry
        // one - so the trail is written in a second save, with a transaction holding the
        // two together. Ranges committing without their audit rows would leave the trail
        // quietly incomplete, which is the one failure an audit trail cannot have.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        RecordImportedRanges(inserted, revivals, revivedFrom, lookups);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Ids are generated now, so build the response conflicts.
        foreach (var (entity, stagedRowNumber, diffs) in stagedConflicts)
        {
            result.Conflicts.Add(new BinConflict
            {
                PendingBinConflictId = entity.PendingBinConflictId,
                RowNumber = stagedRowNumber,
                Prefix = entity.Prefix,
                Differences = diffs
            });
        }

        result.ConflictCount = result.Conflicts.Count;
        result.ImportHistoryId = history.ImportHistoryId;
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

        var conflicts = await _db.Set<PendingBinConflict>()
            .Where(c => ids.Contains(c.PendingBinConflictId) && c.Status == ConflictStatus.Pending)
            .ToListAsync(cancellationToken);

        var targetIds = conflicts.Select(c => c.TargetBinRangeId).ToList();
        var targets = await _db.BinRanges
            .Where(b => targetIds.Contains(b.BinRangeId))
            .ToDictionaryAsync(b => b.BinRangeId, cancellationToken);

        var found = conflicts.Select(c => c.PendingBinConflictId).ToHashSet();
        result.NotFoundCount = ids.Count(id => !found.Contains(id));

        var now = DateTime.UtcNow;

        // Applied conflicts are the only place an existing BIN range is updated - import
        // itself only inserts and stages. Count the updates each originating import
        // ultimately produced, keyed by that import's history row, so ImportHistory.UpdatedRows
        // reflects reality once the conflicts it raised are resolved.
        var updatesByHistory = new Dictionary<int, int>();

        // Only needed to name the ids in the audit snapshots.
        var lookups = conflicts.Count > 0
            ? await Lookups.LoadAsync(_db, cancellationToken)
            : null;

        foreach (var conflict in conflicts)
        {
            if (decisions[conflict.PendingBinConflictId] &&
                targets.TryGetValue(conflict.TargetBinRangeId, out var target))
            {
                // Applying a conflict overwrites live BIN data, so the values it replaces
                // are captured before they are gone.
                var before = Snapshot(target, lookups!);

                target.CardSchemeId = conflict.CardSchemeId;
                target.ProductTypeId = conflict.ProductTypeId;
                target.FundingTypeId = conflict.FundingTypeId;
                target.CountryId = conflict.CountryId;
                target.PrefixLength = conflict.PrefixLength;
                target.ValidFrom = conflict.ValidFrom;
                target.ValidTo = conflict.ValidTo;
                target.UpdatedAt = now;
                target.UpdatedBy = _currentUser.Name;

                _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, target.BinRangeId,
                    before, Snapshot(target, lookups!));

                conflict.Status = ConflictStatus.Applied;
                result.UpdatedCount++;
                updatesByHistory[conflict.ImportHistoryId] =
                    updatesByHistory.GetValueOrDefault(conflict.ImportHistoryId) + 1;
            }
            else
            {
                // Discarding changes no BIN range, so there is nothing to snapshot. Who
                // decided, and when, is recorded on the conflict itself just below.
                conflict.Status = ConflictStatus.Discarded;
                result.DiscardedCount++;
            }

            conflict.ResolvedAt = now;
            conflict.ResolvedBy = _currentUser.Name;
        }

        if (updatesByHistory.Count > 0)
        {
            var historyIds = updatesByHistory.Keys.ToList();
            var histories = await _db.ImportHistories
                .Where(h => historyIds.Contains(h.ImportHistoryId))
                .ToListAsync(cancellationToken);

            foreach (var history in histories)
            {
                history.UpdatedRows += updatesByHistory[history.ImportHistoryId];
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<List<BinConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
    {
        var lookups = await Lookups.LoadAsync(_db, cancellationToken);

        // Read-only projection for display, so nothing here needs change tracking.
        var pending = await _db.Set<PendingBinConflict>()
            .AsNoTracking()
            .Where(c => c.Status == ConflictStatus.Pending)
            .OrderBy(c => c.PendingBinConflictId)
            .ToListAsync(cancellationToken);

        var targetIds = pending.Select(c => c.TargetBinRangeId).Distinct().ToList();
        var targets = await _db.BinRanges
            .AsNoTracking()
            .Where(b => targetIds.Contains(b.BinRangeId))
            .ToDictionaryAsync(b => b.BinRangeId, cancellationToken);

        var conflicts = new List<BinConflict>();
        foreach (var conflict in pending)
        {
            // If the target row is gone the conflict is stale; skip it.
            if (!targets.TryGetValue(conflict.TargetBinRangeId, out var target))
                continue;

            var incoming = new ResolvedRow(
                conflict.Prefix, conflict.CardSchemeId, conflict.ProductTypeId,
                conflict.FundingTypeId, conflict.CountryId, conflict.ValidFrom, conflict.ValidTo);

            conflicts.Add(new BinConflict
            {
                PendingBinConflictId = conflict.PendingBinConflictId,
                RowNumber = 0, // not meaningful outside the originating file
                Prefix = conflict.Prefix,
                Differences = Diff(target, incoming, lookups)
            });
        }

        return conflicts;
    }

    /// <summary>
    /// Writes the audit trail for everything an import changed.
    /// <para>
    /// Only the rows that touched live BIN data are recorded. A rejected row changed
    /// nothing and is already kept, with its reason, on the import history; an unchanged
    /// row by definition changed nothing; and a staged conflict has not been decided yet,
    /// so it is audited if and when someone applies it.
    /// </para>
    /// </summary>
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

    /// <summary>
    /// Describes a stored range for the audit trail, resolving its lookup ids through the
    /// snapshot the import already loaded rather than going back to the database per row.
    /// </summary>
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

    /// <summary>
    /// Structural validation only. Returns the first rule that fails so the message
    /// stays specific. Lookup existence is checked separately against the database.
    /// </summary>
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

    /// <summary>
    /// Hand-rolled character scans. LINQ's <c>All</c> would allocate an enumerator for
    /// every field of every row; these run allocation-free on the parse hot path.
    /// </summary>
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

    /// <summary>
    /// Compares an incoming row against the existing record, returning a diff entry
    /// for every field that differs (formatted for display). An empty list means the
    /// records are identical and the row can be skipped.
    /// </summary>
    private static List<BinFieldDiff> Diff(BinRange current, ResolvedRow incoming, Lookups lookups)
    {
        var diffs = new List<BinFieldDiff>();

        if (current.CardSchemeId != incoming.CardSchemeId)
            diffs.Add(new BinFieldDiff
            {
                Field = "CardScheme",
                OldValue = lookups.CardSchemeName(current.CardSchemeId),
                NewValue = lookups.CardSchemeName(incoming.CardSchemeId)
            });

        if (current.ProductTypeId != incoming.ProductTypeId)
            diffs.Add(new BinFieldDiff
            {
                Field = "ProductType",
                OldValue = lookups.ProductTypeName(current.ProductTypeId),
                NewValue = lookups.ProductTypeName(incoming.ProductTypeId)
            });

        if (current.FundingTypeId != incoming.FundingTypeId)
            diffs.Add(new BinFieldDiff
            {
                Field = "FundingType",
                OldValue = lookups.FundingTypeName(current.FundingTypeId),
                NewValue = lookups.FundingTypeName(incoming.FundingTypeId)
            });

        if (current.CountryId != incoming.CountryId)
            diffs.Add(new BinFieldDiff
            {
                Field = "CountryCode",
                OldValue = lookups.CountryCode(current.CountryId),
                NewValue = lookups.CountryCode(incoming.CountryId)
            });

        if (current.ValidFrom != incoming.ValidFrom)
            diffs.Add(new BinFieldDiff
            {
                Field = "ValidFrom",
                OldValue = current.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture),
                NewValue = incoming.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture)
            });

        if (current.ValidTo != incoming.ValidTo)
            diffs.Add(new BinFieldDiff
            {
                Field = "ValidTo",
                OldValue = current.ValidTo?.ToString(DateFormat, CultureInfo.InvariantCulture),
                NewValue = incoming.ValidTo?.ToString(DateFormat, CultureInfo.InvariantCulture)
            });

        return diffs;
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

        await _db.SaveChangesAsync(cancellationToken);
        
        result.ImportHistoryId = history.ImportHistoryId;
    }

    private sealed record Candidate(int RowNumber, string Raw, ResolvedRow Values);

    /// <summary>
    /// Column positions in the file being read, resolved once from the header.
    /// <see cref="ValidTo"/> is -1 when the optional column is absent.
    /// </summary>
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

    /// <summary>
    /// A structurally-valid row before its lookup names are resolved to ids. A struct
    /// so the parse loop does not allocate one object per row.
    /// </summary>
    private readonly record struct ParsedRow(
        string Prefix, string CardScheme, string ProductType, string FundingType,
        string CountryCode, DateTime ValidFrom, DateTime? ValidTo);

    /// <summary>
    /// A validated import row with its lookup names resolved to database ids.
    /// </summary>
    private sealed record ResolvedRow(
        string Prefix, int CardSchemeId, int ProductTypeId, int FundingTypeId, int CountryId,
        DateTime ValidFrom, DateTime? ValidTo);

    /// <summary>
    /// In-memory snapshot of the lookup tables, keyed for resolution (name/code to id)
    /// and for diffing (id back to display value).
    /// </summary>
    private sealed class Lookups
    {
        private readonly Dictionary<string, int> _cardSchemes;
        private readonly Dictionary<string, int> _productTypes;
        private readonly Dictionary<string, int> _fundingTypes;
        private readonly Dictionary<string, int> _countries;
        private readonly Dictionary<int, string> _cardSchemeNames;
        private readonly Dictionary<int, string> _productTypeNames;
        private readonly Dictionary<int, string> _fundingTypeNames;
        private readonly Dictionary<int, string> _countryCodes;

        private Lookups(
            Dictionary<string, int> cardSchemes, Dictionary<string, int> productTypes,
            Dictionary<string, int> fundingTypes, Dictionary<string, int> countries,
            Dictionary<int, string> cardSchemeNames, Dictionary<int, string> productTypeNames,
            Dictionary<int, string> fundingTypeNames, Dictionary<int, string> countryCodes)
        {
            _cardSchemes = cardSchemes;
            _productTypes = productTypes;
            _fundingTypes = fundingTypes;
            _countries = countries;
            _cardSchemeNames = cardSchemeNames;
            _productTypeNames = productTypeNames;
            _fundingTypeNames = fundingTypeNames;
            _countryCodes = countryCodes;
        }

        public static async Task<Lookups> LoadAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            var cardSchemes = await db.CardSchemes.Where(c => !c.IsDeleted)
                .Select(c => new { c.CardSchemeId, c.Name }).ToListAsync(cancellationToken);
            var productTypes = await db.ProductTypes.Where(p => !p.IsDeleted)
                .Select(p => new { p.ProductTypeId, p.Name }).ToListAsync(cancellationToken);
            var fundingTypes = await db.FundingTypes.Where(f => !f.IsDeleted)
                .Select(f => new { f.FundingTypeId, f.Name }).ToListAsync(cancellationToken);
            var countries = await db.Countries.Where(c => !c.IsDeleted)
                .Select(c => new { c.CountryId, c.IsoCode }).ToListAsync(cancellationToken);

            return new Lookups(
                cardSchemes.ToDictionary(c => c.Name, c => c.CardSchemeId, StringComparer.OrdinalIgnoreCase),
                productTypes.ToDictionary(p => p.Name, p => p.ProductTypeId, StringComparer.OrdinalIgnoreCase),
                fundingTypes.ToDictionary(f => f.Name, f => f.FundingTypeId, StringComparer.OrdinalIgnoreCase),
                countries.ToDictionary(c => c.IsoCode, c => c.CountryId, StringComparer.OrdinalIgnoreCase),
                cardSchemes.ToDictionary(c => c.CardSchemeId, c => c.Name),
                productTypes.ToDictionary(p => p.ProductTypeId, p => p.Name),
                fundingTypes.ToDictionary(f => f.FundingTypeId, f => f.Name),
                countries.ToDictionary(c => c.CountryId, c => c.IsoCode));
        }

        public bool TryResolve(in ParsedRow row, out ResolvedRow resolved, out string reason)
        {
            resolved = default!;
            reason = string.Empty;

            if (!_cardSchemes.TryGetValue(row.CardScheme, out var cardSchemeId))
            {
                reason = $"CardScheme '{row.CardScheme}' does not exist";
                return false;
            }

            if (!_productTypes.TryGetValue(row.ProductType, out var productTypeId))
            {
                reason = $"ProductType '{row.ProductType}' does not exist";
                return false;
            }

            if (!_fundingTypes.TryGetValue(row.FundingType, out var fundingTypeId))
            {
                reason = $"FundingType '{row.FundingType}' does not exist";
                return false;
            }

            if (!_countries.TryGetValue(row.CountryCode, out var countryId))
            {
                reason = $"CountryCode '{row.CountryCode}' does not exist";
                return false;
            }

            resolved = new ResolvedRow(
                row.Prefix, cardSchemeId, productTypeId, fundingTypeId, countryId,
                row.ValidFrom, row.ValidTo);
            return true;
        }

        public string? CardSchemeName(int id) => _cardSchemeNames.GetValueOrDefault(id);
        public string? ProductTypeName(int id) => _productTypeNames.GetValueOrDefault(id);
        public string? FundingTypeName(int id) => _fundingTypeNames.GetValueOrDefault(id);
        public string? CountryCode(int id) => _countryCodes.GetValueOrDefault(id);
    }
}
