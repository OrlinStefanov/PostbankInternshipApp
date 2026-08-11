namespace BinTool.Application.Models.Audit;

/// <summary>
/// What a country looked like at one moment, as stored in an audit entry's
/// <c>OldValues</c> / <c>NewValues</c>. The region is written by name rather than by id
/// so the entry keeps saying what the country belonged to even if the reference tables
/// are renumbered or the region is renamed later.
/// </summary>
public sealed record CountrySnapshot(
    string IsoCode, string Name, string Region, bool IsDeleted);
