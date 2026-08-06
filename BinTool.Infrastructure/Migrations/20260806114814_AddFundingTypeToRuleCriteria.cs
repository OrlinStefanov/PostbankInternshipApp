using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingTypeToRuleCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RuleCriteria_FundingTypes_FundingTypeId",
                table: "RuleCriteria");

            migrationBuilder.DropIndex(
                name: "IX_RuleCriteria_Lookup",
                table: "RuleCriteria");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCriteria_Lookup",
                table: "RuleCriteria",
                columns: new[] { "CardSchemeId", "ProductTypeId", "FundingTypeId", "RegionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RuleCriteria_FundingTypes_FundingTypeId",
                table: "RuleCriteria",
                column: "FundingTypeId",
                principalTable: "FundingTypes",
                principalColumn: "FundingTypeId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RuleCriteria_FundingTypes_FundingTypeId",
                table: "RuleCriteria");

            migrationBuilder.DropIndex(
                name: "IX_RuleCriteria_Lookup",
                table: "RuleCriteria");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCriteria_Lookup",
                table: "RuleCriteria",
                columns: new[] { "CardSchemeId", "ProductTypeId", "RegionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RuleCriteria_FundingTypes_FundingTypeId",
                table: "RuleCriteria",
                column: "FundingTypeId",
                principalTable: "FundingTypes",
                principalColumn: "FundingTypeId");
        }
    }
}
