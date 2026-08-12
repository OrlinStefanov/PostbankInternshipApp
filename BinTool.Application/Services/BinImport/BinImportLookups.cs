using BinTool.Application.Models.Import;

namespace BinTool.Application.Services.BinImport;

internal sealed class BinImportLookups
{
    private readonly ReferenceTables _tables;

    public BinImportLookups(ReferenceTables tables)
    {
        _tables = tables;
    }

    public bool TryResolve(in ParsedRow row, out ResolvedRow resolved, out string reason)
    {
        resolved = default!;
        reason = string.Empty;

        if (!_tables.CardSchemeIds.TryGetValue(row.CardScheme, out var cardSchemeId))
        {
            reason = $"CardScheme '{row.CardScheme}' does not exist";
            return false;
        }

        if (!_tables.ProductTypeIds.TryGetValue(row.ProductType, out var productTypeId))
        {
            reason = $"ProductType '{row.ProductType}' does not exist";
            return false;
        }

        if (!_tables.FundingTypeIds.TryGetValue(row.FundingType, out var fundingTypeId))
        {
            reason = $"FundingType '{row.FundingType}' does not exist";
            return false;
        }

        if (!_tables.CountryIds.TryGetValue(row.CountryCode, out var countryId))
        {
            reason = $"CountryCode '{row.CountryCode}' does not exist";
            return false;
        }

        resolved = new ResolvedRow(
            row.Prefix, cardSchemeId, productTypeId, fundingTypeId, countryId,
            row.ValidFrom, row.ValidTo);

        return true;
    }

    public string? CardSchemeName(int id) => _tables.CardSchemeNames.GetValueOrDefault(id);
    public string? ProductTypeName(int id) => _tables.ProductTypeNames.GetValueOrDefault(id);
    public string? FundingTypeName(int id) => _tables.FundingTypeNames.GetValueOrDefault(id);
    public string? CountryCode(int id) => _tables.CountryCodes.GetValueOrDefault(id);
}
