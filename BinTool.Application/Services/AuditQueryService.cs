using BinTool.Application.Abstractions;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using BinTool.Domain.Entities;

namespace BinTool.Application.Services;

public class AuditQueryService : IAuditQueryService
{
    private static readonly IReadOnlyList<string> EntityTypes = new[]
    {
        AuditEntityTypes.BinRange,
        AuditEntityTypes.CardScheme,
        AuditEntityTypes.ProductType,
        AuditEntityTypes.FundingType,
        AuditEntityTypes.Region,
        AuditEntityTypes.Country,
        AuditEntityTypes.CommissionRule,
        AuditEntityTypes.DefaultRule,
        AuditEntityTypes.Role,
        AuditEntityTypes.UserRole
    };

    private readonly IAuditRepository _audit;

    public AuditQueryService(IAuditRepository audit)
    {
        _audit = audit;
    }

    public Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(
            query.PageSize <= 0 ? AuditQuery.DefaultPageSize : query.PageSize,
            1, AuditQuery.MaxPageSize);

        return _audit.SearchAsync(query, page, pageSize, cancellationToken);
    }

    public IReadOnlyList<string> GetEntityTypes() => EntityTypes;
}
