using BinTool.Api.Services;
using BinTool.Application.Services;
using BinTool.Infrastructure.Repositories;

namespace BinTool.Api.Extensions;

public static class ApplicationServiceExtensions
{
    // Written out one by one rather than scanned off the assembly, so the lifetime of each is
    // visible here instead of implied by a convention - the singletons in particular hold no
    // per-request state.
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
        services.AddScoped<IBinRangeRepository, BinRangeRepository>();
        services.AddScoped<IBinImportRepository, BinImportRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IImportHistoryRepository, ImportHistoryRepository>();

        services.AddScoped<ICredentialStore, CredentialStore>();

        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<ISignInService, SignInService>();

        return services;
    }
}
