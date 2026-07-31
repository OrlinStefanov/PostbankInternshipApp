using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BinTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    FullName = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    ChangedProperties = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ChangeReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ClientInfo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
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
                name: "CommissionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Scheme = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductType = table.Column<int>(type: "INTEGER", nullable: true),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: true),
                    PercentageRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: false),
                    MinimumFee = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionRules_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Countries_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BinRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Scheme = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductType = table.Column<int>(type: "INTEGER", nullable: false),
                    FundingType = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinRanges_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Regions",
                columns: new[] { "Id", "Description", "Name", "Type" },
                values: new object[,]
                {
                    { 1, "Domestic transactions (same country)", "Domestic", 1 },
                    { 2, "Transactions within European Economic Area (subject to interchange caps)", "Intra-EEA", 2 },
                    { 3, "Transactions outside EEA (not subject to interchange caps)", "Inter-Regional", 3 },
                    { 4, "Region could not be determined", "Unknown", 0 }
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "RegionId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "BG", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7268), "Bulgaria", 1, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7268) },
                    { 2, "AT", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7271), "Austria", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7271) },
                    { 3, "BE", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7273), "Belgium", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7273) },
                    { 4, "HR", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7275), "Croatia", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7275) },
                    { 5, "CY", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7277), "Cyprus", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7277) },
                    { 6, "CZ", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7279), "Czech Republic", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7279) },
                    { 7, "DK", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7281), "Denmark", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7281) },
                    { 8, "EE", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7347), "Estonia", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7347) },
                    { 9, "FI", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7349), "Finland", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7349) },
                    { 10, "FR", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7351), "France", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7351) },
                    { 11, "DE", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7353), "Germany", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7353) },
                    { 12, "GR", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7355), "Greece", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7355) },
                    { 13, "HU", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7356), "Hungary", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7357) },
                    { 14, "IE", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7358), "Ireland", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7359) },
                    { 15, "IT", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7360), "Italy", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7360) },
                    { 16, "LV", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7362), "Latvia", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7362) },
                    { 17, "LT", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7364), "Lithuania", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7364) },
                    { 18, "LU", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7366), "Luxembourg", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7366) },
                    { 19, "MT", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7367), "Malta", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7368) },
                    { 20, "NL", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7369), "Netherlands", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7370) },
                    { 21, "PL", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7371), "Poland", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7372) },
                    { 22, "PT", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7373), "Portugal", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7374) },
                    { 23, "RO", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7375), "Romania", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7375) },
                    { 24, "SK", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7377), "Slovakia", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7377) },
                    { 25, "SI", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7378), "Slovenia", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7379) },
                    { 26, "ES", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7380), "Spain", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7381) },
                    { 27, "SE", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7382), "Sweden", 2, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7382) },
                    { 28, "GB", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7384), "United Kingdom", 3, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7384) },
                    { 29, "US", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7386), "United States", 3, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7386) },
                    { 30, "CA", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7388), "Canada", 3, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7388) },
                    { 31, "JP", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7389), "Japan", 3, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7390) },
                    { 32, "CN", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7391), "China", 3, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7391) },
                    { 33, "IN", new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7393), "India", 3, new DateTime(2026, 7, 31, 11, 56, 38, 25, DateTimeKind.Utc).AddTicks(7393) }
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
                columns: new[] { "EntityType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_UserId",
                table: "AuditEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BinRange_Prefix_Scheme",
                table: "BinRanges",
                columns: new[] { "Prefix", "Scheme" });

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_CountryId",
                table: "BinRanges",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_Prefix",
                table: "BinRanges",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRule_Active",
                table: "CommissionRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRule_Lookup",
                table: "CommissionRules",
                columns: new[] { "Scheme", "ProductType", "RegionId", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_RegionId",
                table: "CommissionRules",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code",
                table: "Countries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_RegionId",
                table: "Countries",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_Type",
                table: "Regions",
                column: "Type",
                unique: true);
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
                name: "CommissionRules");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Regions");
        }
    }
}
