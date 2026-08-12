namespace BinTool.Application.Services.BinImport;

internal sealed record ResolvedRow(
    string Prefix, int CardSchemeId, int ProductTypeId, int FundingTypeId, int CountryId,
    DateTime ValidFrom, DateTime? ValidTo);
