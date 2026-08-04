using System.Text;
using BinTool.Core.Models.Import;
using BinTool.Infrastructure.Services;

namespace BinTool.Tests;

/// <summary>
/// Fixture for the import tests: the shared SQLite database from <see cref="SqliteTestBase"/>
/// plus helpers for feeding CSV content through the service.
/// </summary>
public abstract class ImportTestBase : SqliteTestBase
{
    protected const string Header =
        "Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo";

    protected readonly BinCsvImportService Service;

    protected ImportTestBase()
    {
        Service = new BinCsvImportService(Db);
    }

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
}
