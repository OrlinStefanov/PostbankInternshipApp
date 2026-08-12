using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Abstractions;

public interface ILookupAdminService
{
    /// <summary>Lists the rows for one kind, ordered by name.</summary>
    Task<List<LookupListItem>> SearchAsync(
        LookupKind kind, bool includeDeleted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one row by id, deleted rows included, or null when nothing matches.
    /// </summary>
    Task<LookupListItem?> GetAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    Task<LookupMutationResult> CreateAsync(
        LookupKind kind, LookupInput input, CancellationToken cancellationToken = default);

    Task<LookupMutationResult> UpdateAsync(
        LookupKind kind, int id, LookupInput input, CancellationToken cancellationToken = default);

    Task<LookupMutationResult> DeleteAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    Task<LookupMutationResult> RestoreAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);
}
