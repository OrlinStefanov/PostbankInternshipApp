using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Models.Currency;

public class CurrencyListItem
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal RateToEur { get; set; }

    public bool IsActive { get; set; }

    public LookupStatus Status { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
