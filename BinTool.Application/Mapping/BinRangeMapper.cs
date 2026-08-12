using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Mapping;

public static class BinRangeMapper
{
    public static string NormalizedPrefix(this BinRangeInput input) => input.Prefix.Trim();

    public static void Apply(
        BinRange range, BinRangeInput input, ResolvedReferences resolved, string user, DateTime now)
    {
        var prefix = input.NormalizedPrefix();

        range.Prefix = prefix;
        range.PrefixLength = prefix.Length;
        range.CardSchemeId = resolved.CardScheme.Id;
        range.ProductTypeId = resolved.ProductType.Id;
        range.FundingTypeId = resolved.FundingType.Id;
        range.CountryId = resolved.Country.Id;
        range.ValidFrom = input.ValidFrom.Date;
        range.ValidTo = input.ValidTo?.Date;
        range.UpdatedAt = now;
        range.UpdatedBy = user;
    }

    public static BinRangeSnapshot ToSnapshot(
        BinRangeInput input, ResolvedReferences resolved, bool isDeleted) =>
        new(input.NormalizedPrefix(),
            resolved.CardScheme.Name,
            resolved.ProductType.Name,
            resolved.FundingType.Name,
            resolved.Country.Name,
            BinRangeSnapshot.Date(input.ValidFrom.Date),
            input.ValidTo is null ? null : BinRangeSnapshot.Date(input.ValidTo.Value.Date),
            isDeleted);

    public static void Undelete(BinRange range)
    {
        range.IsDeleted = false;
        range.DeletedAt = null;
        range.DeletedBy = null;
    }
}

public readonly record struct ResolvedReferences(
    NamedReference CardScheme,
    NamedReference ProductType,
    NamedReference FundingType,
    NamedReference Country);
