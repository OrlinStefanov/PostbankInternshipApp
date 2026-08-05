using BinTool.Core.Models.BinRanges;

namespace BinTool.Core.Services;

/// <summary>
/// Maintains BIN ranges one at a time, for the cases a CSV import does not cover:
/// a single correction, a range that arrives outside the file, or withdrawing one.
/// <para>
/// Every method records who made the change, and deletes are soft so a range can be
/// brought back with its history intact.
/// </para>
/// </summary>
public interface IBinRangeAdminService
{
    /// <summary>
    /// Returns one range by id, deleted ones included, or null if there is no such range.
    /// </summary>
    Task<BinRangeListItem?> GetAsync(int binRangeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a range. If the prefix exists only as a soft-deleted row, that row is revived
    /// with the supplied values instead - the prefix is unique across deleted rows, so
    /// there is no second record to insert.
    /// </summary>
    Task<BinRangeMutationResult> CreateAsync(
        BinRangeInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites a range with the supplied values. The prefix may be changed, as long as
    /// no other range already owns it.
    /// </summary>
    Task<BinRangeMutationResult> UpdateAsync(
        int binRangeId, BinRangeInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a range. It stops matching classification and drops out of the default
    /// listing, but stays in the database and can be restored.
    /// </summary>
    Task<BinRangeMutationResult> DeleteAsync(
        int binRangeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Brings a soft-deleted range back, with the values it had when it was deleted.
    /// </summary>
    Task<BinRangeMutationResult> RestoreAsync(
        int binRangeId, CancellationToken cancellationToken = default);
}
