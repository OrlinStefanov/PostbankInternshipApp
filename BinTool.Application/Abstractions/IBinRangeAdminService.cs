using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

public interface IBinRangeAdminService
{
    /// <summary>
    /// Returns one range by id, deleted ones included, or null if there is no such range.
    /// </summary>
    Task<BinRangeListItem?> GetAsync(int binRangeId, CancellationToken cancellationToken = default);

    /// <summary>Adds a range.</summary>
    Task<BinRangeMutationResult> CreateAsync(
        BinRangeInput input, CancellationToken cancellationToken = default);

    /// <summary>Overwrites a range with the supplied values.</summary>
    Task<BinRangeMutationResult> UpdateAsync(
        int binRangeId, BinRangeInput input, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a range.</summary>
    Task<BinRangeMutationResult> DeleteAsync(
        int binRangeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Brings a soft-deleted range back, with the values it had when it was deleted.
    /// </summary>
    Task<BinRangeMutationResult> RestoreAsync(
        int binRangeId, CancellationToken cancellationToken = default);
}
