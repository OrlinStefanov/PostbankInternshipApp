using BinTool.Core.Models.Audit;
using BinTool.Core.Models.BinRanges;

namespace BinTool.Core.Services;

public interface IAuditQueryService
{
    /// <summary>
    /// Returns one page of audit entries matching the filters, newest first, with the
    /// user id resolved to a user name.
    /// </summary>
    Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the entity-type values the log currently records - the entries the
    /// EntityType filter can take. Sourced from <see cref="Entities.AuditEntityTypes"/>
    /// rather than the data, so a filter option exists even before its first row.
    /// </summary>
    IReadOnlyList<string> GetEntityTypes();
}
