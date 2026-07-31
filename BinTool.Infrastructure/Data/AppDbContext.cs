using BinTool.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Data;

/// <summary>
/// Main database context for the BinTool application
/// Includes identity management and domain entities
/// </summary>
public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    #region DbSets - Lookup Tables

    /// <summary>
    /// Card payment schemes (Visa, Mastercard, etc.)
    /// </summary>
    public DbSet<CardScheme> CardSchemes { get; set; }

    /// <summary>
    /// Card product types (Consumer, Commercial, Prepaid)
    /// </summary>
    public DbSet<ProductType> ProductTypes { get; set; }

    /// <summary>
    /// Card funding types (Credit, Debit)
    /// </summary>
    public DbSet<FundingType> FundingTypes { get; set; }

    /// <summary>
    /// Geographic regions for fee determination
    /// </summary>
    public DbSet<Region> Regions { get; set; }

    #endregion

    #region DbSets - Core Domain

    /// <summary>
    /// Countries with region assignment
    /// </summary>
    public DbSet<Country> Countries { get; set; }

    /// <summary>
    /// Bank Identification Number (BIN) ranges
    /// </summary>
    public DbSet<BinRange> BinRanges { get; set; }

    #endregion

    #region DbSets - Commission Rules

    /// <summary>
    /// Commission rules for fee calculation
    /// </summary>
    public DbSet<CommissionRule> CommissionRules { get; set; }

    /// <summary>
    /// Criteria for commission rules (which cards they apply to)
    /// </summary>
    public DbSet<RuleCriteria> RuleCriteria { get; set; }

    /// <summary>
    /// Designates default commission rule
    /// </summary>
    public DbSet<DefaultRule> DefaultRules { get; set; }

    #endregion

    #region DbSets - Import & Audit

    /// <summary>
    /// Audit trail of all configuration changes
    /// </summary>
    public DbSet<AuditEntry> AuditEntries { get; set; }

    /// <summary>
    /// History of bulk import operations
    /// </summary>
    public DbSet<ImportHistory> ImportHistories { get; set; }

    /// <summary>
    /// Rows rejected during import
    /// </summary>
    public DbSet<RejectedImportRow> RejectedImportRows { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCardScheme(modelBuilder);
        ConfigureProductType(modelBuilder);
        ConfigureFundingType(modelBuilder);
        ConfigureRegion(modelBuilder);
        ConfigureCountry(modelBuilder);
        ConfigureBinRange(modelBuilder);
        ConfigureCommissionRule(modelBuilder);
        ConfigureRuleCriteria(modelBuilder);
        ConfigureDefaultRule(modelBuilder);
        ConfigureAuditEntry(modelBuilder);
        ConfigureImportHistory(modelBuilder);
        ConfigureRejectedImportRow(modelBuilder);

        SeedReferenceData(modelBuilder);
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
            entity.HasIndex(e => new { e.CardSchemeId, e.ProductTypeId, e.RegionId })
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

            entity.HasIndex(e => e.ImportedAt)
                .HasDatabaseName("IX_ImportHistory_Date");

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

    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
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

        // Seed Countries (sample list)
        modelBuilder.Entity<Country>().HasData(
            // Bulgaria (Domestic)
            new Country { CountryId = 1, IsoCode = "BG", Name = "Bulgaria", RegionId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            // EEA Countries
            new Country { CountryId = 2, IsoCode = "AT", Name = "Austria", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 3, IsoCode = "BE", Name = "Belgium", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 4, IsoCode = "FR", Name = "France", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 5, IsoCode = "DE", Name = "Germany", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 6, IsoCode = "IT", Name = "Italy", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 7, IsoCode = "ES", Name = "Spain", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 8, IsoCode = "NL", Name = "Netherlands", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 9, IsoCode = "SE", Name = "Sweden", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 10, IsoCode = "GB", Name = "United Kingdom", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            // Non-EEA
            new Country { CountryId = 11, IsoCode = "US", Name = "United States", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 12, IsoCode = "CA", Name = "Canada", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 13, IsoCode = "JP", Name = "Japan", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { CountryId = 14, IsoCode = "CN", Name = "China", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
    }
}
