using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Abstractions;

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
