using BinTool.Domain.Entities;

namespace BinTool.Application.Services.BinImport;

internal static class PendingBinConflictMapper
{
    public static PendingBinConflict From(
        ResolvedRow v, string raw, ImportHistory history, DateTime now, ConflictType type) => new()
    {
        ConflictType = type,
        Prefix = v.Prefix,
        PrefixLength = v.Prefix.Length,
        CardSchemeId = v.CardSchemeId,
        ProductTypeId = v.ProductTypeId,
        FundingTypeId = v.FundingTypeId,
        CountryId = v.CountryId,
        ValidFrom = v.ValidFrom,
        ValidTo = v.ValidTo,
        RawData = raw,
        Status = ConflictStatus.Pending,
        CreatedAt = now,
        ImportHistory = history
    };
}
