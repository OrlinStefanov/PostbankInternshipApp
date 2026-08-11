using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Models.Currency;

/// <summary>
/// One currency as a browse listing shows it: the code and name, the euro rate, and the
/// derived (soft-delete) status.
/// </summary>
public class CurrencyListItem
{
    public int Id { get; set; }

    /// <summary>ISO-4217 code, e.g. "EUR".</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>The euro value of one unit of this currency. Euro is 1.0.</summary>
    public decimal RateToEur { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Derived from the soft-delete flag, not stored.</summary>
    public LookupStatus Status { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
