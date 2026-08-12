using BinTool.Application.Models.Audit;
using BinTool.Domain.Entities;

namespace BinTool.Application.Services.BinImport;

internal static class BinRangeSnapshotMapper
{
    public static BinRangeSnapshot From(BinRange range, BinImportLookups lookups) => new(
        range.Prefix,
        lookups.CardSchemeName(range.CardSchemeId),
        lookups.ProductTypeName(range.ProductTypeId),
        lookups.FundingTypeName(range.FundingTypeId),
        lookups.CountryCode(range.CountryId),
        BinRangeSnapshot.Date(range.ValidFrom),
        range.ValidTo is null ? null : BinRangeSnapshot.Date(range.ValidTo.Value),
        range.IsDeleted);

    public static BinRangeSnapshot From(ResolvedRow row, BinImportLookups lookups, bool isDeleted) => new(
        row.Prefix,
        lookups.CardSchemeName(row.CardSchemeId),
        lookups.ProductTypeName(row.ProductTypeId),
        lookups.FundingTypeName(row.FundingTypeId),
        lookups.CountryCode(row.CountryId),
        BinRangeSnapshot.Date(row.ValidFrom),
        row.ValidTo is null ? null : BinRangeSnapshot.Date(row.ValidTo.Value),
        isDeleted);
}
