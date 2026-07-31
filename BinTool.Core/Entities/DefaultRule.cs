namespace BinTool.Core.Entities;

/// <summary>
/// Designates which commission rule is the system default
/// Used when no specific rule matches a transaction
/// </summary>
public class DefaultRule
{
    public int DefaultRuleId { get; set; }

    /// <summary>
    /// The commission rule that serves as default
    /// </summary>
    public int CommissionRuleId { get; set; }

    /// <summary>
    /// Whether this is the system default rule
    /// Only one rule should have this set to true
    /// </summary>
    public bool IsSystemDefault { get; set; }

    #region Navigation Properties

    public CommissionRule? CommissionRule { get; set; }

    #endregion
}
