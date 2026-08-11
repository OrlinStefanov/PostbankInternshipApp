using System.Text;
using BinTool.Application.Abstractions;
using BinTool.Application.Models.Import;
using BinTool.Application.Services;
using BinTool.Infrastructure.Repositories;
using BinTool.Tests.Fakes;

namespace BinTool.Tests;

public abstract class ImportTestBase : SqliteTestBase
{
    protected const string Header =
        "Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo";

    protected readonly BinCsvImportService Service;

    protected readonly RecordingLogger<BinCsvImportService> ImportLogger = new();

    protected ICurrentUser CurrentUser = new SystemCurrentUser();

    protected ImportTestBase()
    {
        // Resolved lazily through a wrapper so a test can replace CurrentUser after the
        // base constructor has already built the service.
        var currentUser = new DeferredCurrentUser(() => CurrentUser);

        Service = new BinCsvImportService(
            new BinImportRepository(Db), currentUser, new AuditLog(new AuditRepository(Db), currentUser),
            new CardSchemeDetector(), ImportLogger);
    }

    private sealed class DeferredCurrentUser : ICurrentUser
    {
        private readonly Func<ICurrentUser> _resolve;

        public DeferredCurrentUser(Func<ICurrentUser> resolve)
        {
            _resolve = resolve;
        }

        public string? UserId => _resolve().UserId;

        public string Name => _resolve().Name;
    }

    protected Task<BinImportResult> Run(params string[] dataRows) =>
        RunWithHeader(Header, dataRows);

    protected Task<BinImportResult> RunWithHeader(string header, params string[] dataRows)
    {
        var content = new StringBuilder().AppendLine(header);
        foreach (var row in dataRows)
            content.AppendLine(row);

        return RunRaw(content.ToString());
    }

    protected Task<BinImportResult> RunRaw(string csv, string fileName = "test.csv") =>
        RunBytes(Encoding.UTF8.GetBytes(csv), fileName);

    protected Task<BinImportResult> RunBytes(byte[] csv, string fileName = "test.csv") =>
        Service.ImportAsync(new MemoryStream(csv), fileName);
}
