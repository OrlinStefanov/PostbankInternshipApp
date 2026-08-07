namespace BinTool.Core.Entities;

/// <summary>
/// A currency that commission amounts can be denominated in, with the rate used to express
/// those amounts in euro. Euro is the base (rate 1); a fixed-peg currency such as the
/// Bulgarian lev keeps its legally set rate. One current rate per currency - there is no
/// historical rate table, so a fee is always converted at the rate stored today.
/// </summary>
public class Currency
{
    public int CurrencyId { get; set; }

    /// <summary>ISO-4217 code, upper-case three letters, e.g. "EUR". Unique.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name, e.g. "Euro".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The euro value of one unit of this currency. Euro is 1.0; the lev is 1/1.95583 ≈
    /// 0.511292. An amount in this currency is converted to euro by multiplying by this rate.
    /// </summary>
    public decimal RateToEur { get; set; }

    /// <summary>Whether the currency is offered for new rules and lookups.</summary>
    public bool IsActive { get; set; } = true;

    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    #endregion

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>Commission rules denominated in this currency.</summary>
    public ICollection<CommissionRule> CommissionRules { get; set; } = new List<CommissionRule>();

    #endregion
}
