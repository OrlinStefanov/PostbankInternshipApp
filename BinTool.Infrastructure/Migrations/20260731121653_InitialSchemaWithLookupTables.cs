using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BinTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemaWithLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    AuditEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    PerformedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.AuditEntryId);
                });

            migrationBuilder.CreateTable(
                name: "CardSchemes",
                columns: table => new
                {
                    CardSchemeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSchemes", x => x.CardSchemeId);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                columns: table => new
                {
                    CommissionRuleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RuleName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PercentageRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    MinimumFee = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.CommissionRuleId);
                });

            migrationBuilder.CreateTable(
                name: "FundingTypes",
                columns: table => new
                {
                    FundingTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingTypes", x => x.FundingTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ImportHistories",
                columns: table => new
                {
                    ImportHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ImportedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportHistories", x => x.ImportHistoryId);
                });

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                columns: table => new
                {
                    ProductTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.ProductTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.RegionId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefaultRules",
                columns: table => new
                {
                    DefaultRuleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommissionRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultRules", x => x.DefaultRuleId);
                    table.ForeignKey(
                        name: "FK_DefaultRules_CommissionRules_CommissionRuleId",
                        column: x => x.CommissionRuleId,
                        principalTable: "CommissionRules",
                        principalColumn: "CommissionRuleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RejectedImportRows",
                columns: table => new
                {
                    RejectedRowId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportHistoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RawData = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectedImportRows", x => x.RejectedRowId);
                    table.ForeignKey(
                        name: "FK_RejectedImportRows_ImportHistories_ImportHistoryId",
                        column: x => x.ImportHistoryId,
                        principalTable: "ImportHistories",
                        principalColumn: "ImportHistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    CountryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsoCode = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryId);
                    table.ForeignKey(
                        name: "FK_Countries_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "RegionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleCriteria",
                columns: table => new
                {
                    RuleCriteriaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommissionRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    CardSchemeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: true),
                    PriorityScore = table.Column<int>(type: "INTEGER", nullable: false),
                    FundingTypeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleCriteria", x => x.RuleCriteriaId);
                    table.ForeignKey(
                        name: "FK_RuleCriteria_CardSchemes_CardSchemeId",
                        column: x => x.CardSchemeId,
                        principalTable: "CardSchemes",
                        principalColumn: "CardSchemeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RuleCriteria_CommissionRules_CommissionRuleId",
                        column: x => x.CommissionRuleId,
                        principalTable: "CommissionRules",
                        principalColumn: "CommissionRuleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleCriteria_FundingTypes_FundingTypeId",
                        column: x => x.FundingTypeId,
                        principalTable: "FundingTypes",
                        principalColumn: "FundingTypeId");
                    table.ForeignKey(
                        name: "FK_RuleCriteria_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "ProductTypeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RuleCriteria_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "RegionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BinRanges",
                columns: table => new
                {
                    BinRangeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    PrefixLength = table.Column<int>(type: "INTEGER", nullable: false),
                    CardSchemeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FundingTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinRanges", x => x.BinRangeId);
                    table.ForeignKey(
                        name: "FK_BinRanges_CardSchemes_CardSchemeId",
                        column: x => x.CardSchemeId,
                        principalTable: "CardSchemes",
                        principalColumn: "CardSchemeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BinRanges_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BinRanges_FundingTypes_FundingTypeId",
                        column: x => x.FundingTypeId,
                        principalTable: "FundingTypes",
                        principalColumn: "FundingTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BinRanges_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "ProductTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CardSchemes",
                columns: new[] { "CardSchemeId", "DeletedAt", "DeletedBy", "Description", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Visa card scheme", false, "Visa" },
                    { 2, null, null, "Mastercard and Maestro", false, "Mastercard" },
                    { 3, null, null, "American Express", false, "American Express" },
                    { 4, null, null, "Diners Club", false, "Diners Club" }
                });

            migrationBuilder.InsertData(
                table: "FundingTypes",
                columns: new[] { "FundingTypeId", "DeletedAt", "DeletedBy", "Description", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Credit cards", false, "Credit" },
                    { 2, null, null, "Debit cards", false, "Debit" }
                });

            migrationBuilder.InsertData(
                table: "ProductTypes",
                columns: new[] { "ProductTypeId", "DeletedAt", "DeletedBy", "Description", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Consumer cards", false, "Consumer" },
                    { 2, null, null, "Commercial/Business cards", false, "Commercial" },
                    { 3, null, null, "Prepaid cards", false, "Prepaid" }
                });

            migrationBuilder.InsertData(
                table: "Regions",
                columns: new[] { "RegionId", "DeletedAt", "DeletedBy", "Description", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Domestic transactions (same country)", false, "Domestic" },
                    { 2, null, null, "Transactions within European Economic Area (subject to interchange caps)", false, "Intra-EEA" },
                    { 3, null, null, "Transactions outside EEA (not subject to interchange caps)", false, "Inter-Regional" }
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "CountryId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsDeleted", "IsoCode", "Name", "RegionId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4264), null, null, null, false, "BG", "Bulgaria", 1, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4265), null },
                    { 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4267), null, null, null, false, "AT", "Austria", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4268), null },
                    { 3, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4269), null, null, null, false, "BE", "Belgium", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4270), null },
                    { 4, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4271), null, null, null, false, "FR", "France", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4272), null },
                    { 5, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4273), null, null, null, false, "DE", "Germany", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4274), null },
                    { 6, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4275), null, null, null, false, "IT", "Italy", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4276), null },
                    { 7, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4277), null, null, null, false, "ES", "Spain", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4278), null },
                    { 8, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4279), null, null, null, false, "NL", "Netherlands", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4279), null },
                    { 9, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4281), null, null, null, false, "SE", "Sweden", 2, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4281), null },
                    { 10, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4283), null, null, null, false, "GB", "United Kingdom", 3, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4284), null },
                    { 11, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4285), null, null, null, false, "US", "United States", 3, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4286), null },
                    { 12, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4287), null, null, null, false, "CA", "Canada", 3, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4288), null },
                    { 13, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4289), null, null, null, false, "JP", "Japan", 3, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4290), null },
                    { 14, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4291), null, null, null, false, "CN", "China", 3, new DateTime(2026, 7, 31, 12, 16, 52, 949, DateTimeKind.Utc).AddTicks(4292), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_Type_Timestamp",
                table: "AuditEntries",
                columns: new[] { "EntityType", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_UserId",
                table: "AuditEntries",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BinRange_Prefix_Scheme",
                table: "BinRanges",
                columns: new[] { "Prefix", "CardSchemeId" });

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_CardSchemeId",
                table: "BinRanges",
                column: "CardSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_CountryId",
                table: "BinRanges",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_FundingTypeId",
                table: "BinRanges",
                column: "FundingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_Prefix",
                table: "BinRanges",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_ProductTypeId",
                table: "BinRanges",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CardSchemes_Name",
                table: "CardSchemes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRule_IsActive",
                table: "CommissionRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRule_Priority",
                table: "CommissionRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_IsoCode",
                table: "Countries",
                column: "IsoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_RegionId",
                table: "Countries",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_DefaultRules_CommissionRuleId",
                table: "DefaultRules",
                column: "CommissionRuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundingTypes_Name",
                table: "FundingTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportHistory_Date",
                table: "ImportHistories",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_Name",
                table: "ProductTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Regions_Name",
                table: "Regions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RejectedImportRows_ImportHistoryId",
                table: "RejectedImportRows",
                column: "ImportHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCriteria_CommissionRuleId",
                table: "RuleCriteria",
                column: "CommissionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCriteria_FundingTypeId",
                table: "RuleCriteria",
                column: "FundingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCriteria_Lookup",
                table: "RuleCriteria",
                columns: new[] { "CardSchemeId", "ProductTypeId", "RegionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleCriteria_ProductTypeId",
                table: "RuleCriteria",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCriteria_RegionId",
                table: "RuleCriteria",
                column: "RegionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "BinRanges");

            migrationBuilder.DropTable(
                name: "DefaultRules");

            migrationBuilder.DropTable(
                name: "RejectedImportRows");

            migrationBuilder.DropTable(
                name: "RuleCriteria");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "ImportHistories");

            migrationBuilder.DropTable(
                name: "CardSchemes");

            migrationBuilder.DropTable(
                name: "CommissionRules");

            migrationBuilder.DropTable(
                name: "FundingTypes");

            migrationBuilder.DropTable(
                name: "ProductTypes");

            migrationBuilder.DropTable(
                name: "Regions");
        }
    }
}
