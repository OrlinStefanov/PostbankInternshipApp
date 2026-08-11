namespace BinTool.Domain.Entities;

public enum ConflictStatus
{
    Pending = 0,
    Applied = 1,
    Discarded = 2,
}

public enum ConflictType
{
    ValueConflict = 0,

    // The declared card scheme contradicts the network the prefix belongs to (or the prefix matches
    // no known network at all). May or may not also overwrite an existing range, so
    // TargetBinRangeId can be null.
    SchemeMismatch = 1,
}

// A structurally-valid import row whose prefix already exists in the database with different
// values. It is parked here at import time so a user can later decide, per row, whether to
// overwrite the existing BinRange (Applied) or keep the current record (Discarded).
public class PendingBinConflict
{
    public int PendingBinConflictId { get; set; }

    public int ImportHistoryId { get; set; }

    public int? TargetBinRangeId { get; set; }

    public ConflictType ConflictType { get; set; } = ConflictType.ValueConflict;

    public string Prefix { get; set; } = string.Empty;

    public int PrefixLength { get; set; }

    #region Proposed Values (from the CSV, already resolved to ids)

    public int CardSchemeId { get; set; }

    public int ProductTypeId { get; set; }

    public int FundingTypeId { get; set; }

    public int CountryId { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    #endregion

    public string RawData { get; set; } = string.Empty;

    public ConflictStatus Status { get; set; } = ConflictStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public string? ResolvedBy { get; set; }

    #region Navigation Properties

    public ImportHistory? ImportHistory { get; set; }

    public BinRange? TargetBinRange { get; set; }

    #endregion
}
