using BinTool.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    #region DbSets - Lookup Tables

    public DbSet<CardScheme> CardSchemes { get; set; }

    public DbSet<ProductType> ProductTypes { get; set; }

    public DbSet<FundingType> FundingTypes { get; set; }

    public DbSet<Region> Regions { get; set; }

    public DbSet<Currency> Currencies { get; set; }

    #endregion

    #region DbSets - Core Domain

    public DbSet<Country> Countries { get; set; }

    public DbSet<BinRange> BinRanges { get; set; }

    #endregion

    #region DbSets - Commission Rules

    public DbSet<CommissionRule> CommissionRules { get; set; }

    public DbSet<RuleCriteria> RuleCriteria { get; set; }

    public DbSet<DefaultRule> DefaultRules { get; set; }

    #endregion

    #region DbSets - Import & Audit

    public DbSet<AuditEntry> AuditEntries { get; set; }

    public DbSet<ImportHistory> ImportHistories { get; set; }

    public DbSet<RejectedImportRow> RejectedImportRows { get; set; }

    public DbSet<PendingBinConflict> PendingBinConflicts { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentity(modelBuilder);
        ConfigureCardScheme(modelBuilder);
        ConfigureProductType(modelBuilder);
        ConfigureFundingType(modelBuilder);
        ConfigureRegion(modelBuilder);
        ConfigureCurrency(modelBuilder);
        ConfigureCountry(modelBuilder);
        ConfigureBinRange(modelBuilder);
        ConfigureCommissionRule(modelBuilder);
        ConfigureRuleCriteria(modelBuilder);
        ConfigureDefaultRule(modelBuilder);
        ConfigureAuditEntry(modelBuilder);
        ConfigureImportHistory(modelBuilder);
        ConfigureRejectedImportRow(modelBuilder);
        ConfigurePendingBinConflict(modelBuilder);

        SeedReferenceData(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_ApplicationUser_IsActive");
        });

        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    private static void ConfigureCardScheme(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardScheme>(entity =>
        {
            entity.HasKey(e => e.CardSchemeId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.ToTable("CardSchemes");
        });
    }

    private static void ConfigureProductType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.HasKey(e => e.ProductTypeId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Name)
                .IsUnique();


            entity.ToTable("ProductTypes");
        });
    }

    private static void ConfigureFundingType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FundingType>(entity =>
        {
            entity.HasKey(e => e.FundingTypeId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Name)
                .IsUnique();


            entity.ToTable("FundingTypes");
        });
    }

    private static void ConfigureRegion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.RegionId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Name)
                .IsUnique();


            entity.ToTable("Regions");
        });
    }

    private static void ConfigureCurrency(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.CurrencyId);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.RateToEur)
                .HasPrecision(18, 10);

            entity.HasIndex(e => e.Code)
                .IsUnique();

            entity.ToTable("Currencies");
        });
    }

    private static void ConfigureCountry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.CountryId);

            entity.Property(e => e.IsoCode)
                .IsRequired()
                .HasMaxLength(2)
                .IsFixedLength();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.IsoCode)
                .IsUnique();

            entity.HasOne(e => e.Region)
                .WithMany(r => r.Countries)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();


            entity.ToTable("Countries");
        });
    }

    private static void ConfigureBinRange(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BinRange>(entity =>
        {
            entity.HasKey(e => e.BinRangeId);

            entity.Property(e => e.Prefix)
                .IsRequired()
                .HasMaxLength(8);

            entity.Property(e => e.PrefixLength)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on prefix
            entity.HasIndex(e => e.Prefix)
                .IsUnique();

            // Critical index for prefix-based lookups
            entity.HasIndex(e => new { e.Prefix, e.CardSchemeId })
                .HasDatabaseName("IX_BinRange_Prefix_Scheme");

            entity.HasOne(e => e.CardScheme)
                .WithMany(cs => cs.BinRanges)
                .HasForeignKey(e => e.CardSchemeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne(e => e.ProductType)
                .WithMany(pt => pt.BinRanges)
                .HasForeignKey(e => e.ProductTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne(e => e.FundingType)
                .WithMany(ft => ft.BinRanges)
                .HasForeignKey(e => e.FundingTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne(e => e.Country)
                .WithMany(c => c.BinRanges)
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();


            entity.ToTable("BinRanges");
        });
    }

    private static void ConfigureCommissionRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommissionRule>(entity =>
        {
            entity.HasKey(e => e.CommissionRuleId);

            entity.Property(e => e.RuleName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.PercentageRate)
                .HasPrecision(5, 4);

            entity.Property(e => e.FixedAmount)
                .HasPrecision(10, 4);

            entity.Property(e => e.MinimumFee)
                .HasPrecision(10, 4);

            entity.Property(e => e.Priority)
                .IsRequired();

            entity.Property(e => e.ValidFrom)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_CommissionRule_IsActive");

            entity.HasIndex(e => e.Priority)
                .HasDatabaseName("IX_CommissionRule_Priority");

            // Existing rows predate the currency column; default them to euro (id 1) so the
            // migration backfills a valid foreign key rather than a zero.
            entity.Property(e => e.CurrencyId)
                .HasDefaultValue(1);

            // A rule is always denominated in exactly one currency. Restrict, not cascade:
            // a currency in use cannot be removed out from under a rule (the service refuses
            // the delete before it gets here).
            entity.HasOne(e => e.Currency)
                .WithMany(c => c.CommissionRules)
                .HasForeignKey(e => e.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.ToTable("CommissionRules");
        });
    }

    private static void ConfigureRuleCriteria(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RuleCriteria>(entity =>
        {
            entity.HasKey(e => e.RuleCriteriaId);

            entity.Property(e => e.PriorityScore)
                .IsRequired();

            // Index for rule resolution lookups
            entity.HasIndex(e => new { e.CardSchemeId, e.ProductTypeId, e.FundingTypeId, e.RegionId })
                .HasDatabaseName("IX_RuleCriteria_Lookup");

            entity.HasOne(e => e.CommissionRule)
                .WithMany(cr => cr.RuleCriteria)
                .HasForeignKey(e => e.CommissionRuleId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne(e => e.CardScheme)
                .WithMany(cs => cs.RuleCriteria)
                .HasForeignKey(e => e.CardSchemeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ProductType)
                .WithMany(pt => pt.RuleCriteria)
                .HasForeignKey(e => e.ProductTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.FundingType)
                .WithMany(ft => ft.RuleCriteria)
                .HasForeignKey(e => e.FundingTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Region)
                .WithMany(r => r.RuleCriteria)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable("RuleCriteria");
        });
    }

    private static void ConfigureDefaultRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DefaultRule>(entity =>
        {
            entity.HasKey(e => e.DefaultRuleId);

            entity.Property(e => e.IsSystemDefault)
                .IsRequired();

            entity.HasOne(e => e.CommissionRule)
                .WithOne(cr => cr.DefaultRule)
                .HasForeignKey<DefaultRule>(e => e.CommissionRuleId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.ToTable("DefaultRules");
        });
    }

    private static void ConfigureAuditEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.AuditEntryId);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.OldValues)
                .HasColumnType("TEXT");

            entity.Property(e => e.NewValues)
                .HasColumnType("TEXT");

            entity.Property(e => e.PerformedByUserId)
                .HasMaxLength(450);

            entity.Property(e => e.PerformedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes for audit querying
            entity.HasIndex(e => new { e.EntityType, e.PerformedAt })
                .HasDatabaseName("IX_AuditEntry_Type_Timestamp");

            entity.HasIndex(e => e.PerformedByUserId)
                .HasDatabaseName("IX_AuditEntry_UserId");

            // Keep the audit trail intact if the user is ever removed:
            // the row survives with a null user reference.
            entity.HasOne(e => e.PerformedByUser)
                .WithMany(u => u.AuditEntries)
                .HasForeignKey(e => e.PerformedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable("AuditEntries");
        });
    }

    private static void ConfigureImportHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportHistory>(entity =>
        {
            entity.HasKey(e => e.ImportHistoryId);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ImportedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ImportedByUserId)
                .HasMaxLength(450);

            entity.HasIndex(e => e.ImportedAt)
                .HasDatabaseName("IX_ImportHistory_Date");

            entity.HasOne(e => e.ImportedByUser)
                .WithMany(u => u.Imports)
                .HasForeignKey(e => e.ImportedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.RejectedRows_Navigation)
                .WithOne(r => r.ImportHistory)
                .HasForeignKey(r => r.ImportHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("ImportHistories");
        });
    }

    private static void ConfigureRejectedImportRow(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RejectedImportRow>(entity =>
        {
            entity.HasKey(e => e.RejectedRowId);

            entity.Property(e => e.RowNumber)
                .IsRequired();

            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.RawData)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.HasOne(e => e.ImportHistory)
                .WithMany(ih => ih.RejectedRows_Navigation)
                .HasForeignKey(e => e.ImportHistoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.ToTable("RejectedImportRows");
        });
    }

    private static void ConfigurePendingBinConflict(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PendingBinConflict>(entity =>
        {
            entity.HasKey(e => e.PendingBinConflictId);

            entity.Property(e => e.Prefix)
                .IsRequired()
                .HasMaxLength(8);

            entity.Property(e => e.PrefixLength)
                .IsRequired();

            entity.Property(e => e.RawData)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.ConflictType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.ResolvedBy)
                .HasMaxLength(450);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Fast lookup of the outstanding conflicts.
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_PendingBinConflict_Status");

            entity.HasOne(e => e.ImportHistory)
                .WithMany()
                .HasForeignKey(e => e.ImportHistoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Keep the staged conflict even if the target row is hard-deleted; the
            // resolution step re-checks the target still exists before applying.
            // Optional: a scheme mismatch on a new prefix has no existing target and
            // applies by inserting a fresh range instead.
            entity.HasOne(e => e.TargetBinRange)
                .WithMany()
                .HasForeignKey(e => e.TargetBinRangeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.ToTable("PendingBinConflicts");
        });
    }

    private static readonly DateTime SeedTimestamp = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        // Seed Roles. Ids and concurrency stamps are fixed so the seed stays stable
        // across migrations.
        modelBuilder.Entity<ApplicationRole>().HasData(
            new ApplicationRole
            {
                Id = "11111111-1111-1111-1111-111111111111",
                Name = AppRoles.Admin,
                NormalizedName = AppRoles.Admin.ToUpperInvariant(),
                ConcurrencyStamp = "11111111-1111-1111-1111-111111111111",
                Description = "Full access: manage BIN ranges, commission rules, users and imports",
                CreatedAt = SeedTimestamp
            },
            new ApplicationRole
            {
                Id = "22222222-2222-2222-2222-222222222222",
                Name = AppRoles.Viewer,
                NormalizedName = AppRoles.Viewer.ToUpperInvariant(),
                ConcurrencyStamp = "22222222-2222-2222-2222-222222222222",
                Description = "Read-only access: browse BIN ranges, rules and audit history",
                CreatedAt = SeedTimestamp
            }
        );

        // Seed Card Schemes
        modelBuilder.Entity<CardScheme>().HasData(
            new CardScheme { CardSchemeId = 1, Name = "Visa", Description = "Visa card scheme" },
            new CardScheme { CardSchemeId = 2, Name = "Mastercard", Description = "Mastercard and Maestro" },
            new CardScheme { CardSchemeId = 3, Name = "American Express", Description = "American Express" },
            new CardScheme { CardSchemeId = 4, Name = "Diners Club", Description = "Diners Club" }
        );

        // Seed Product Types
        modelBuilder.Entity<ProductType>().HasData(
            new ProductType { ProductTypeId = 1, Name = "Consumer", Description = "Consumer cards" },
            new ProductType { ProductTypeId = 2, Name = "Commercial", Description = "Commercial/Business cards" },
            new ProductType { ProductTypeId = 3, Name = "Prepaid", Description = "Prepaid cards" }
        );

        // Seed Funding Types
        modelBuilder.Entity<FundingType>().HasData(
            new FundingType { FundingTypeId = 1, Name = "Credit", Description = "Credit cards" },
            new FundingType { FundingTypeId = 2, Name = "Debit", Description = "Debit cards" }
        );

        // Seed Regions
        modelBuilder.Entity<Region>().HasData(
            new Region { RegionId = 1, Name = "Domestic", Description = "Domestic transactions (same country)" },
            new Region { RegionId = 2, Name = "Intra-EEA", Description = "Transactions within European Economic Area (subject to interchange caps)" },
            new Region { RegionId = 3, Name = "Inter-Regional", Description = "Transactions outside EEA (not subject to interchange caps)" }
        );

        // Seed Currencies. Euro is the base (rate 1) and the default for new rules; the lev
        // keeps its fixed peg (1 EUR = 1.95583 BGN, so 1 BGN = 1/1.95583 EUR) so legacy
        // lev-priced rules can still be shown in euro.
        modelBuilder.Entity<Currency>().HasData(
            new Currency
            {
                CurrencyId = 1, Code = "EUR", Name = "Euro", RateToEur = 1m, IsActive = true,
                CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp
            },
            new Currency
            {
                CurrencyId = 2, Code = "BGN", Name = "Bulgarian lev", RateToEur = 0.5112918788m,
                IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp
            }
        );

        // Seed Countries (sample list)
        modelBuilder.Entity<Country>().HasData(
            // Bulgaria (Domestic)
            new Country { CountryId = 1, IsoCode = "BG", Name = "Bulgaria", RegionId = 1, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            // EEA Countries
            new Country { CountryId = 2, IsoCode = "AT", Name = "Austria", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 3, IsoCode = "BE", Name = "Belgium", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 4, IsoCode = "FR", Name = "France", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 5, IsoCode = "DE", Name = "Germany", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 6, IsoCode = "IT", Name = "Italy", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 7, IsoCode = "ES", Name = "Spain", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 8, IsoCode = "NL", Name = "Netherlands", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 9, IsoCode = "SE", Name = "Sweden", RegionId = 2, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 10, IsoCode = "GB", Name = "United Kingdom", RegionId = 3, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            // Non-EEA
            new Country { CountryId = 11, IsoCode = "US", Name = "United States", RegionId = 3, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 12, IsoCode = "CA", Name = "Canada", RegionId = 3, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 13, IsoCode = "JP", Name = "Japan", RegionId = 3, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new Country { CountryId = 14, IsoCode = "CN", Name = "China", RegionId = 3, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp }
        );
    }
}
