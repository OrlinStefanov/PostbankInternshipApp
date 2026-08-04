using System.Text;
using BinTool.Core.Entities;
using BinTool.Core.Models.Import;
using BinTool.Infrastructure.Data;
using BinTool.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

/// <summary>
/// Shared fixture for the import tests: an isolated SQLite database seeded with the
/// reference data, plus helpers for feeding CSV content through the service.
/// <para>
/// SQLite rather than the in-memory provider because the import relies on real
/// relational behaviour - unique indexes and foreign keys in particular, which the
/// in-memory provider does not enforce and would let genuine bugs pass as green.
/// The database lives in memory and is torn down with the connection.
/// </para>
/// </summary>
public abstract class ImportTestBase : IDisposable
{
    protected const string Header =
        "Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo";

    // Ids match the seeded reference data applied by EnsureCreated.
    protected const int VisaId = 1, MastercardId = 2, AmexId = 3;
    protected const int ConsumerId = 1, CommercialId = 2;
    protected const int CreditId = 1, DebitId = 2;
    protected const int BgCountryId = 1, UsCountryId = 11;

    // Holding the connection open is what keeps the in-memory database alive; closing
    // it drops the schema and all data.
    private readonly SqliteConnection _connection;

    protected readonly AppDbContext Db;
    protected readonly BinCsvImportService Service;

    protected ImportTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Db = NewContext();
        Db.Database.EnsureCreated(); // builds the schema and seeds the lookup tables
        Service = new BinCsvImportService(Db);
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
    /// Builds a CSV from the standard header plus the given data rows and imports it.
    /// </summary>
    protected Task<BinImportResult> Run(params string[] dataRows) =>
        RunWithHeader(Header, dataRows);

    protected Task<BinImportResult> RunWithHeader(string header, params string[] dataRows)
    {
        var content = new StringBuilder().AppendLine(header);
        foreach (var row in dataRows)
            content.AppendLine(row);

        return RunRaw(content.ToString());
    }

    /// <summary>
    /// Imports the exact string given, so a test can control line endings, encoding
    /// preamble and spacing precisely.
    /// </summary>
    protected Task<BinImportResult> RunRaw(string csv, string fileName = "test.csv") =>
        RunBytes(Encoding.UTF8.GetBytes(csv), fileName);

    protected Task<BinImportResult> RunBytes(byte[] csv, string fileName = "test.csv") =>
        Service.ImportAsync(new MemoryStream(csv), fileName);

    /// <summary>
    /// Seeds an existing BIN range so reconciliation has something to compare against.
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
