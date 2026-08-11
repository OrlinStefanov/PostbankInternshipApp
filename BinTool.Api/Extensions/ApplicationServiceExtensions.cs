using BinTool.Api.Services;
using BinTool.Application.Services;
using BinTool.Infrastructure.Repositories;
using BinTool.Infrastructure.Services;

namespace BinTool.Api.Extensions;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers the application's own services. Written out one by one rather than scanned
    /// off the assembly, so the lifetime of each is visible here instead of implied by a
    /// convention - the singletons in particular hold no per-request state, and that is a
    /// claim worth being able to check at a glance.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();

        services.AddSingleton<ICardSchemeDetector, CardSchemeDetector>();
        services.AddScoped<IBinCsvImportService, BinCsvImportService>();
        services.AddScoped<IImportHistoryQueryService, ImportHistoryQueryService>();
        services.AddScoped<IBinClassificationService, BinClassificationService>();
        services.AddScoped<IBinRangeQueryService, BinRangeQueryService>();
        services.AddScoped<IBinRangeAdminService, BinRangeAdminService>();

        services.AddScoped<ILookupAdminService, LookupAdminService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<ICountryAdminService, CountryAdminService>();

        services.AddScoped<ICommissionRuleAdminService, CommissionRuleAdminService>();
        services.AddScoped<ICommissionResolver, CommissionResolver>();

        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        return services;
    }
}
