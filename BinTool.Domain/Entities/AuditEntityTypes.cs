namespace BinTool.Domain.Entities;

// Canonical values for EntityType. Constants rather than string literals so a query for one
// entity's history cannot miss rows to a typo.
public static class AuditEntityTypes
{
    public const string BinRange = "BinRange";

    public const string Role = "Role";

    public const string UserRole = "UserRole";

    public const string CardScheme = "CardScheme";

    public const string ProductType = "ProductType";

    public const string FundingType = "FundingType";

    public const string Region = "Region";

    public const string Country = "Country";

    public const string CommissionRule = "CommissionRule";

    public const string RuleCriteria = "RuleCriteria";

    public const string DefaultRule = "DefaultRule";

    public const string Currency = "Currency";
}
