using BinTool.Core.Models.ReferenceData;

namespace BinTool.Core.Services;

/// <summary>
/// Maintenance surface for the four Name+Description reference tables (card scheme,
/// product type, funding type, region). Each call takes a <see cref="LookupKind"/> so
/// one service handles all four - the shape is identical, only the underlying table
/// differs.
/// </summary>
public interface ILookupAdminService
{
    /// <summary>
    /// Lists the rows for one kind, ordered by name.
    /// </summary>
    /// <param name="kind">Which reference table to read.</param>
    /// <param name="includeDeleted">When true, soft-deleted rows are included.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<LookupListItem>> SearchAsync(
        LookupKind kind, bool includeDeleted, CancellationToken cancellationToken = default);

    /// <summary>Returns one row by id, deleted rows included, or null when nothing matches.</summary>
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
