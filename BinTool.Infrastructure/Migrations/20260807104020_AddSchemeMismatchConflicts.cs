using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemeMismatchConflicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TargetBinRangeId",
                table: "PendingBinConflicts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "ConflictType",
                table: "PendingBinConflicts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConflictType",
                table: "PendingBinConflicts");

            migrationBuilder.AlterColumn<int>(
                name: "TargetBinRangeId",
                table: "PendingBinConflicts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
