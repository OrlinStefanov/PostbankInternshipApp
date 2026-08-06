namespace BinTool.Core.Models.Audit;

/// <summary>
/// What a Name+Description reference row looked like at one moment, as stored in an
/// audit entry's <c>OldValues</c> / <c>NewValues</c>. Kept as text so the trail remains
/// readable if ids are ever renumbered.
/// </summary>
public sealed record LookupSnapshot(string Name, string? Description, bool IsDeleted);
