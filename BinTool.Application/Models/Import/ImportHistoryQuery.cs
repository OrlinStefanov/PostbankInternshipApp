namespace BinTool.Application.Models.Import;

public class ImportHistoryQuery
{
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 25;

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;
}
