using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Maintenance surface for the country reference table. Countries are shaped
/// differently from the other reference rows (ISO code + region assignment + audit
/// fields), so they get their own service rather than sharing one with the named
/// lookups.
/// </summary>
public interface ICountryAdminService
{
    Task<List<CountryListItem>> SearchAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    Task<CountryListItem?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<CountryMutationResult> CreateAsync(
        CountryInput input, CancellationToken cancellationToken = default);

    Task<CountryMutationResult> UpdateAsync(
        int id, CountryInput input, CancellationToken cancellationToken = default);

    Task<CountryMutationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<CountryMutationResult> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
