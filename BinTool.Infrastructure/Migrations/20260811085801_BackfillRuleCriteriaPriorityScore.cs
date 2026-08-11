using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinTool.Infrastructure.Migrations
{
    /// <summary>
    /// One-time backfill: sets PriorityScore to the count of non-null key columns for every
    /// existing RuleCriteria row. Safe now because the field has never been editable and no
    /// deliberate overrides exist yet. Must not be re-run once admin overrides are in use.
    /// </summary>
    public partial class BackfillRuleCriteriaPriorityScore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE RuleCriteria SET PriorityScore =
                  (CASE WHEN CardSchemeId  IS NULL THEN 0 ELSE 1 END)
                + (CASE WHEN ProductTypeId IS NULL THEN 0 ELSE 1 END)
                + (CASE WHEN FundingTypeId IS NULL THEN 0 ELSE 1 END)
                + (CASE WHEN RegionId      IS NULL THEN 0 ELSE 1 END);
                """);
        }

        // Down is a no-op: the previous values were the same computed numbers or zeroes,
        // and nothing on the old code path reads the stored column.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
