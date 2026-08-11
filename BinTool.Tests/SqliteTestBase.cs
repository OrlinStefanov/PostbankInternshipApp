using BinTool.Domain.Entities;
using BinTool.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

/// <summary>
/// An isolated SQLite database seeded with the reference data.
/// <para>
/// SQLite rather than the in-memory provider because the code under test relies on real
/// relational behaviour - unique indexes and foreign keys in particular, which the
/// in-memory provider does not enforce and would let genuine bugs pass as green.
/// The database lives in memory and is torn down with the connection.
/// </para>
/// </summary>
public abstract class SqliteTestBase : IDisposable
{
    // Ids match the seeded reference data applied by EnsureCreated.
    protected const int VisaId = 1, MastercardId = 2, AmexId = 3;
    protected const int ConsumerId = 1, CommercialId = 2;
    protected const int CreditId = 1, DebitId = 2;
    protected const int BgCountryId = 1, DeCountryId = 5, UsCountryId = 11;

    // Holding the connection open is what keeps the in-memory database alive; closing
    // it drops the schema and all data.
    private readonly SqliteConnection _connection;

    protected readonly AppDbContext Db;

    protected SqliteTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Db = NewContext();
        Db.Database.EnsureCreated(); // builds the schema and seeds the lookup tables
    }

    /// <summary>
    /// A fresh context over the same database, for asserting what was actually
    /// persisted or for simulating a later request against stored state.
    /// </summary>
    protected AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);

    /// <summary>
    /// Seeds an application user. Audit entries carry a foreign key to the user who made
    /// the change, and SQLite enforces it - so a test that expects a change to be
    /// attributed needs the account to exist.
    /// </summary>
    protected ApplicationUser SeedUser(string id, string userName)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@bintool.local",
            NormalizedEmail = $"{userName}@bintool.local".ToUpperInvariant(),
            FullName = userName,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    /// <summary>
    /// Seeds an existing BIN range so a test has something to match or compare against.
    /// </summary>
    protected BinRange SeedBinRange(string prefix, int cardSchemeId, int productTypeId,
        int fundingTypeId, int countryId, DateTime validFrom, DateTime? validTo = null)
    {
        var range = new BinRange
        {
            Prefix = prefix,
            PrefixLength = prefix.Length,
            CardSchemeId = cardSchemeId,
            ProductTypeId = productTypeId,
            FundingTypeId = fundingTypeId,
            CountryId = countryId,
            ValidFrom = validFrom,
            ValidTo = validTo
        };

        Db.BinRanges.Add(range);
        Db.SaveChanges();
        return range;
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
