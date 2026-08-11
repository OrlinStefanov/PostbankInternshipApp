using BinTool.Api.Extensions;
using BinTool.Api.Options;
using BinTool.Api.Services;
using BinTool.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BinTool.Tests;

/// <summary>
/// Proves the container can actually build what the application asks of it.
/// <para>
/// A missing registration compiles perfectly well and only fails when a request arrives, so
/// nothing else here would catch one. That matters most while services are being split into
/// repositories: every extraction adds an interface that has to be registered, and forgetting
/// one is the obvious way to break the app without breaking a test.
/// </para>
/// </summary>
public class ServiceRegistrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public ServiceRegistrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        // The pieces Program.cs registers alongside: hosting, configuration, storage and
        // Identity, which the role and user services take their managers from.
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddOptions<JwtOptions>();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityServices();

        // The registration under test.
        services.AddApplicationServices();

        // ValidateOnBuild reports every unresolvable dependency at once rather than the first;
        // ValidateScopes catches a scoped service captured by a singleton, which would quietly
        // hold one request's DbContext for the life of the process.
        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    public static TheoryData<Type> RegisteredServices => new()
    {
        typeof(ICurrentUser),
        typeof(IJwtTokenService),
        typeof(IAuditLog),
        typeof(IAuditQueryService),
        typeof(ICardSchemeDetector),
        typeof(IBinCsvImportService),
        typeof(IImportHistoryQueryService),
        typeof(IBinClassificationService),
        typeof(IBinRangeQueryService),
        typeof(IBinRangeAdminService),
        typeof(ILookupAdminService),
        typeof(ICurrencyService),
        typeof(ICountryAdminService),
        typeof(ICommissionRuleAdminService),
        typeof(ICommissionResolver),
        typeof(ICommissionRuleRepository),
        typeof(IReferenceDataRepository),
        typeof(ICurrencyRepository),
        typeof(ICountryRepository),
        typeof(ILookupRepository),
        typeof(IRoleRepository),
        typeof(IRoleAdminService),
        typeof(IUserAdminService)
    };

    [Theory]
    [MemberData(nameof(RegisteredServices))]
    public void Every_registered_service_can_be_resolved(Type serviceType)
    {
        using var scope = _provider.CreateScope();

        scope.ServiceProvider.GetRequiredService(serviceType).Should().NotBeNull();
    }

    [Fact]
    public void The_commission_rule_service_is_built_from_its_repositories()
    {
        using var scope = _provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ICommissionRuleAdminService>();

        // The concrete type lives in the application layer now, not next to the DbContext.
        service.Should().BeOfType<Application.Services.CommissionRuleAdminService>();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
