using BinTool.Domain.Entities;

namespace BinTool.Application.Models.Audit;

public class AuditQuery
{
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 25;

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public string? EntityType { get; set; }

    public string? UserName { get; set; }

    public AuditAction? Action { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;
}
