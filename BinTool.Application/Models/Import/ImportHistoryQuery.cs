namespace BinTool.Application.Models.Import;

public class ImportHistoryQuery
{
    /// <summary>
    /// Largest page the API will return in one call, so a wide-open query cannot pull the whole
    /// history into memory.
    /// </summary>
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 25;

    /// <summary>Only imports stamped at or after this UTC instant are returned.</summary>
    public DateTime? From { get; set; }

    /// <summary>Only imports stamped strictly before this UTC instant are returned.</summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// One of the recorded statuses - Success, Partial or Failed - matched case-insensitively.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>1-based page number.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Rows per page, capped at <see cref="MaxPageSize"/>.</summary>
    public int PageSize { get; set; } = DefaultPageSize;
}
