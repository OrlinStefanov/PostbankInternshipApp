using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingBinConflicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingBinConflicts",
                columns: table => new
                {
                    PendingBinConflictId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportHistoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetBinRangeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    PrefixLength = table.Column<int>(type: "INTEGER", nullable: false),
                    CardSchemeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FundingTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RawData = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingBinConflicts", x => x.PendingBinConflictId);
                    table.ForeignKey(
                        name: "FK_PendingBinConflicts_BinRanges_TargetBinRangeId",
                        column: x => x.TargetBinRangeId,
                        principalTable: "BinRanges",
                        principalColumn: "BinRangeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingBinConflicts_ImportHistories_ImportHistoryId",
                        column: x => x.ImportHistoryId,
                        principalTable: "ImportHistories",
                        principalColumn: "ImportHistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingBinConflict_Status",
                table: "PendingBinConflicts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingBinConflicts_ImportHistoryId",
                table: "PendingBinConflicts",
                column: "ImportHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingBinConflicts_TargetBinRangeId",
                table: "PendingBinConflicts",
                column: "TargetBinRangeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingBinConflicts");
        }
    }
}
