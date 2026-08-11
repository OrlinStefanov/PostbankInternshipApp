namespace BinTool.Domain.Entities;

/// <summary>
/// Lifecycle of a staged BIN conflict awaiting a human decision.
/// </summary>
public enum ConflictStatus
{
    Pending = 0,
    Applied = 1,
    Discarded = 2,
}

/// <summary>
/// Why a row was staged for review.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// The prefix already exists as a live range with different values.
    /// </summary>
    ValueConflict = 0,

    /// <summary>
    /// The declared card scheme contradicts the network the prefix belongs to (or the
    /// prefix matches no known network at all). May or may not also overwrite an
    /// existing range, so <see cref="PendingBinConflict.TargetBinRangeId"/> can be null.
    /// </summary>
    SchemeMismatch = 1,
}

/// <summary>
/// A structurally-valid import row whose prefix already exists in the database
/// with different values. It is parked here at import time so a user can later
/// decide, per row, whether to overwrite the existing <see cref="BinRange"/>
/// (Applied) or keep the current record (Discarded).
/// </summary>
public class PendingBinConflict
{
    public int PendingBinConflictId { get; set; }

    /// <summary>
    /// Import run that produced this conflict.
    /// </summary>
    public int ImportHistoryId { get; set; }

    /// <summary>
    /// The existing BIN range this conflict would overwrite if applied. Null for a
    /// scheme mismatch on a brand-new prefix, which applies by inserting a new range.
    /// </summary>
    public int? TargetBinRangeId { get; set; }

    /// <summary>
    /// Why the row was staged. Drives how it reads in review; how it is applied is
    /// driven by whether <see cref="TargetBinRangeId"/> is set (update vs insert).
    /// </summary>
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

    /// <summary>
    /// Raw CSV line, kept for inspection.
    /// </summary>
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
