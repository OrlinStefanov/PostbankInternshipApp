using BinTool.Domain.Entities;

namespace BinTool.Application.Models.Audit;

/// <summary>
/// Filters and paging for an audit log search. Every filter is optional and they compose
/// with AND. Left empty, the query returns the latest entries across every entity.
/// </summary>
public class AuditQuery
{
    /// <summary>
    /// Largest page the API will return in one call, so a wide-open query cannot pull
    /// the whole trail into memory.
    /// </summary>
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 25;

    /// <summary>
    /// Only entries stamped at or after this UTC instant are returned.
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// Only entries stamped strictly before this UTC instant are returned. Half-open so
    /// two adjacent day queries never claim the same row twice.
    /// </summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// One of the <see cref="AuditEntityTypes"/> constants, matched case-insensitively.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// User name (Identity <c>UserName</c>) of whoever made the change, starts-with match
    /// and case-insensitive. The literal <c>system</c> selects rows written with no user
    /// signed in.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Restricts the results to one action - Created, Updated, Deleted, Deactivated or
    /// Imported.
    /// </summary>
    public AuditAction? Action { get; set; }

    /// <summary>
    /// 1-based page number. Values below 1 are treated as 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Rows per page, capped at <see cref="MaxPageSize"/>.
    /// </summary>
    public int PageSize { get; set; } = DefaultPageSize;
}
