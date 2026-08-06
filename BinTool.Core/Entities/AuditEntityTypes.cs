namespace BinTool.Core.Entities;

/// <summary>
/// Canonical values for <see cref="AuditEntry.EntityType"/>. Constants rather than string
/// literals so a query for one entity's history cannot miss rows to a typo.
/// </summary>
public static class AuditEntityTypes
{
    public const string BinRange = "BinRange";

    /// <summary>A role's name, description or permissions changed.</summary>
    public const string Role = "Role";

    /// <summary>The set of roles a user holds changed.</summary>
    public const string UserRole = "UserRole";

    /// <summary>Card scheme reference row (Visa, Mastercard, …).</summary>
    public const string CardScheme = "CardScheme";

    /// <summary>Product type reference row (Consumer, Commercial, Prepaid).</summary>
    public const string ProductType = "ProductType";

    /// <summary>Funding type reference row (Credit, Debit).</summary>
    public const string FundingType = "FundingType";

    /// <summary>Region reference row.</summary>
    public const string Region = "Region";

    /// <summary>Country reference row (ISO code, name and region assignment).</summary>
    public const string Country = "Country";

    /// <summary>Commission rule header (name, rates, validity, active flag).</summary>
    public const string CommissionRule = "CommissionRule";

    /// <summary>The scheme/product/region key triple a commission rule matches on.</summary>
    public const string RuleCriteria = "RuleCriteria";

    /// <summary>Which commission rule is the configured fallback default.</summary>
    public const string DefaultRule = "DefaultRule";
}
