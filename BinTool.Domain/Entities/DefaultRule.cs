namespace BinTool.Domain.Entities;

public class DefaultRule
{
    public int DefaultRuleId { get; set; }

    public int CommissionRuleId { get; set; }

    public bool IsSystemDefault { get; set; }

    #region Navigation Properties

    public CommissionRule? CommissionRule { get; set; }

    #endregion
}
