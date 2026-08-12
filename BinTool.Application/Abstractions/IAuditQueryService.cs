using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

public interface IAuditQueryService
{

    Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, CancellationToken cancellationToken = default);
    IReadOnlyList<string> GetEntityTypes();
}
