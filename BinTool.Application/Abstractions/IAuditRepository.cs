using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Storage for the audit trail: the write every service makes as part of its own unit of
/// work, and the browse read behind the audit screen.
/// <para>
/// <see cref="Add"/> does not save. The entry has to land in the same transaction as the
/// change it describes, so it joins whatever the caller is about to commit - an audit row
/// that survives a rolled-back write would be a record of something that never happened.
/// </para>
/// </summary>
public interface IAuditRepository
{
    void Add(AuditEntry entry);

    /// <summary>
    /// One page of the filtered trail. Like the BIN range listing, the projection has to
    /// translate to SQL - resolving the user name per row in memory would be a query per
    /// row - so it comes back as the read model rather than as entities.
    /// </summary>
    Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, int page, int pageSize, CancellationToken cancellationToken = default);
}
