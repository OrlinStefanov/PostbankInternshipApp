namespace BinTool.Core.Entities;

/// <summary>
/// Canonical values for <see cref="AuditEntry.EntityType"/>. Constants rather than string
/// literals so a query for one entity's history cannot miss rows to a typo.
/// </summary>
public static class AuditEntityTypes
{
    public const string BinRange = "BinRange";
}
