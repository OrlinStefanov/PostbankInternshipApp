using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

public interface IAuditRepository
{
    void Add(AuditEntry entry);

    /// <summary>One page of the filtered trail.</summary>
    Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, int page, int pageSize, CancellationToken cancellationToken = default);
}
