using BinTool.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Data;

/// <summary>
/// Main database context for the BinTool application
/// Includes identity management and domain entities
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    #region DbSets

    /// <summary>
    /// Bank Identification Number (BIN) ranges
    /// </summary>
    public DbSet<BinRange> BinRanges { get; set; }

    /// <summary>
    /// Countries and their region assignments
    /// </summary>
    public DbSet<Country> Countries { get; set; }

    /// <summary>
    /// Geographic regions for fee determination
    /// </summary>
    public DbSet<Region> Regions { get; set; }

    /// <summary>
    /// Commission rules for fee calculation
    /// </summary>
    public DbSet<CommissionRule> CommissionRules { get; set; }

    /// <summary>
    /// Audit trail of all configuration changes
    /// </summary>
    public DbSet<AuditEntry> AuditEntries { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRegionEntity(modelBuilder);
        ConfigureCountryEntity(modelBuilder);
        ConfigureBinRangeEntity(modelBuilder);
        ConfigureCommissionRuleEntity(modelBuilder);
        ConfigureAuditEntryEntity(modelBuilder);

        SeedReferenceData(modelBuilder);
    }

    /// <summary>
    /// Configure Region entity mapping and relationships
    /// </summary>
    private static void ConfigureRegionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Type)
                .IsUnique();

            entity.ToTable("Regions");
        });
    }

    /// <summary>
    /// Configure Country entity mapping and relationships
    /// </summary>
    private static void ConfigureCountryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
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

            // Unique constraint on ISO code
            entity.HasIndex(e => e.Code)
                .IsUnique();

            // Foreign key to Region
            entity.HasOne(e => e.Region)
                .WithMany(r => r.Countries)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.ToTable("Countries");
        });
    }

    /// <summary>
    /// Configure BinRange entity mapping and relationships
    /// </summary>
    private static void ConfigureBinRangeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BinRange>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Prefix)
                .IsRequired()
                .HasMaxLength(8);

            entity.Property(e => e.Scheme)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.ProductType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.FundingType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastModifiedBy)
                .HasMaxLength(450);

            // Unique constraint on prefix
            entity.HasIndex(e => e.Prefix)
                .IsUnique();

            // Index for prefix-based lookups (critical for performance)
            entity.HasIndex(e => new { e.Prefix, e.Scheme })
                .HasDatabaseName("IX_BinRange_Prefix_Scheme");

            // Foreign key to Country
            entity.HasOne(e => e.Country)
                .WithMany(c => c.BinRanges)
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.ToTable("BinRanges");
        });
    }

    /// <summary>
    /// Configure CommissionRule entity mapping and relationships
    /// </summary>
    private static void ConfigureCommissionRuleEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommissionRule>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Scheme)
                .HasConversion<int?>();

            entity.Property(e => e.ProductType)
                .HasConversion<int?>();

            entity.Property(e => e.PercentageRate)
                .HasPrecision(5, 4); // Up to 99.9999%

            entity.Property(e => e.FixedAmount)
                .HasPrecision(10, 4); // Large fixed amounts with 4 decimals

            entity.Property(e => e.MinimumFee)
                .HasPrecision(10, 4);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .IsFixedLength();

            entity.Property(e => e.ValidFrom)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastModifiedBy)
                .HasMaxLength(450);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            // Foreign key to Region (optional for wildcards)
            entity.HasOne(e => e.Region)
                .WithMany(r => r.CommissionRules)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for rule resolution lookups
            entity.HasIndex(e => new { e.Scheme, e.ProductType, e.RegionId, e.ValidFrom, e.ValidTo })
                .HasDatabaseName("IX_CommissionRule_Lookup");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_CommissionRule_Active");

            entity.ToTable("CommissionRules");
        });
    }

    /// <summary>
    /// Configure AuditEntry entity mapping
    /// </summary>
    private static void ConfigureAuditEntryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.UserId)
                .HasMaxLength(450);

            entity.Property(e => e.UserName)
                .HasMaxLength(256);

            entity.Property(e => e.OldValues)
                .HasColumnType("TEXT");

            entity.Property(e => e.NewValues)
                .HasColumnType("TEXT");

            entity.Property(e => e.ChangedProperties)
                .HasMaxLength(500);

            entity.Property(e => e.ChangeReason)
                .HasMaxLength(500);

            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ClientInfo)
                .HasMaxLength(500);

            // Index for looking up changes by entity type and time
            entity.HasIndex(e => new { e.EntityType, e.Timestamp })
                .HasDatabaseName("IX_AuditEntry_Type_Timestamp");

            // Index for looking up changes by user
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditEntry_UserId");

            entity.ToTable("AuditEntries");
        });
    }

    /// <summary>
    /// Seed reference data for Regions and Countries
    /// </summary>
    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        // Seed Regions
        modelBuilder.Entity<Region>().HasData(
            new Region { Id = 1, Type = RegionType.Domestic, Name = "Domestic", Description = "Domestic transactions (same country)" },
            new Region { Id = 2, Type = RegionType.IntraEEA, Name = "Intra-EEA", Description = "Transactions within European Economic Area (subject to interchange caps)" },
            new Region { Id = 3, Type = RegionType.InterRegional, Name = "Inter-Regional", Description = "Transactions outside EEA (not subject to interchange caps)" },
            new Region { Id = 4, Type = RegionType.Unknown, Name = "Unknown", Description = "Region could not be determined" }
        );

        // Seed Countries (partial list - add more as needed)
        modelBuilder.Entity<Country>().HasData(
            // Bulgaria (Domestic)
            new Country { Id = 1, Code = "BG", Name = "Bulgaria", RegionId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

            // EEA Countries
            new Country { Id = 2, Code = "AT", Name = "Austria", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 3, Code = "BE", Name = "Belgium", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 4, Code = "HR", Name = "Croatia", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 5, Code = "CY", Name = "Cyprus", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 6, Code = "CZ", Name = "Czech Republic", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 7, Code = "DK", Name = "Denmark", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 8, Code = "EE", Name = "Estonia", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 9, Code = "FI", Name = "Finland", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 10, Code = "FR", Name = "France", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 11, Code = "DE", Name = "Germany", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 12, Code = "GR", Name = "Greece", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 13, Code = "HU", Name = "Hungary", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 14, Code = "IE", Name = "Ireland", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 15, Code = "IT", Name = "Italy", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 16, Code = "LV", Name = "Latvia", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 17, Code = "LT", Name = "Lithuania", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 18, Code = "LU", Name = "Luxembourg", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 19, Code = "MT", Name = "Malta", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 20, Code = "NL", Name = "Netherlands", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 21, Code = "PL", Name = "Poland", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 22, Code = "PT", Name = "Portugal", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 23, Code = "RO", Name = "Romania", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 24, Code = "SK", Name = "Slovakia", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 25, Code = "SI", Name = "Slovenia", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 26, Code = "ES", Name = "Spain", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 27, Code = "SE", Name = "Sweden", RegionId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

            // Post-Brexit UK
            new Country { Id = 28, Code = "GB", Name = "United Kingdom", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

            // Non-EEA examples
            new Country { Id = 29, Code = "US", Name = "United States", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 30, Code = "CA", Name = "Canada", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 31, Code = "JP", Name = "Japan", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 32, Code = "CN", Name = "China", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Country { Id = 33, Code = "IN", Name = "India", RegionId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
    }
}
