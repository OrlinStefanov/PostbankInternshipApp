using BinTool.Application.Abstractions;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Mapping;

public static class CountryMapper
{
    public static string NormalizedIsoCode(this CountryInput input) =>
        input.IsoCode.Trim().ToUpperInvariant();

    public static void Apply(Country country, CountryInput input, RegionIdentity region)
    {
        country.IsoCode = input.NormalizedIsoCode();
        country.Name = input.Name.Trim();
        country.RegionId = region.Id;
    }

    public static CountrySnapshot ToSnapshot(Country c, string regionName) =>
        new(c.IsoCode, c.Name, regionName, c.IsDeleted);

    public static CountrySnapshot ToSnapshot(CountryInput input, RegionIdentity region, bool isDeleted) =>
        new(input.NormalizedIsoCode(), input.Name.Trim(), region.Name, isDeleted);

    public static CountryListItem ToListItem(Country c) => new()
    {
        Id = c.CountryId,
        IsoCode = c.IsoCode,
        Name = c.Name,
        RegionId = c.RegionId,
        RegionName = c.Region?.Name ?? string.Empty,
        Status = c.IsDeleted ? LookupStatus.Deleted : LookupStatus.Active,
        CreatedAt = c.CreatedAt,
        CreatedBy = c.CreatedBy,
        UpdatedAt = c.UpdatedAt,
        UpdatedBy = c.UpdatedBy,
        DeletedAt = c.DeletedAt,
        DeletedBy = c.DeletedBy
    };
}
