using BinTool.Application.Abstractions;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Mapping;

public static class BinRangeMapper
{
    public static string NormalizedPrefix(this BinRangeInput input) => input.Prefix.Trim();

    /// <summary>
    /// Writes the supplied values onto a range. Everything a caller can set goes through
    /// here, so no field is silently left behind when the input model grows.
    /// </summary>
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

    /// <summary>
    /// The after-snapshot, built from the values about to be written and the canonical
    /// names resolving already produced - so it describes the row being saved without
    /// having to read it back first.
    /// </summary>
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

/// <summary>The four references, all of them present. Produced only once every id resolved.</summary>
public readonly record struct ResolvedReferences(
    NamedReference CardScheme,
    NamedReference ProductType,
    NamedReference FundingType,
    NamedReference Country);
