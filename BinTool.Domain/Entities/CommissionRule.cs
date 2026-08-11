namespace BinTool.Domain.Entities;

public class CommissionRule
{
    public int CommissionRuleId { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public int CurrencyId { get; set; } = 1;

    public decimal PercentageRate { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal MinimumFee { get; set; }

    public int Priority { get; set; }

    // Both ends inclusive; a null ValidTo runs forever. DateRange holds the arithmetic.
    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

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

    public Currency? Currency { get; set; }

    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    public DefaultRule? DefaultRule { get; set; }

    #endregion
}
