using BinTool.Domain.Entities;

namespace BinTool.Application.Services.BinImport;

internal static class BinRangeImportMapper
{
    public static BinRange ForImport(ResolvedRow v, DateTime now, string userName) => new()
    {
        Prefix = v.Prefix,
        PrefixLength = v.Prefix.Length,
        CardSchemeId = v.CardSchemeId,
        ProductTypeId = v.ProductTypeId,
        FundingTypeId = v.FundingTypeId,
        CountryId = v.CountryId,
        ValidFrom = v.ValidFrom,
        ValidTo = v.ValidTo,
        CreatedAt = now,
        UpdatedAt = now,
        CreatedBy = userName,
        UpdatedBy = userName
    };

    public static BinRange ForImport(PendingBinConflict c, DateTime now, string userName) => new()
    {
        Prefix = c.Prefix,
        PrefixLength = c.PrefixLength,
        CardSchemeId = c.CardSchemeId,
        ProductTypeId = c.ProductTypeId,
        FundingTypeId = c.FundingTypeId,
        CountryId = c.CountryId,
        ValidFrom = c.ValidFrom,
        ValidTo = c.ValidTo,
        CreatedAt = now,
        UpdatedAt = now,
        CreatedBy = userName,
        UpdatedBy = userName
    };

    public static void ApplyRevival(BinRange target, ResolvedRow v, DateTime now, string userName)
    {
        target.PrefixLength = v.Prefix.Length;
        target.CardSchemeId = v.CardSchemeId;
        target.ProductTypeId = v.ProductTypeId;
        target.FundingTypeId = v.FundingTypeId;
        target.CountryId = v.CountryId;
        target.ValidFrom = v.ValidFrom;
        target.ValidTo = v.ValidTo;
        target.IsDeleted = false;
        target.DeletedAt = null;
        target.DeletedBy = null;
        target.UpdatedAt = now;
        target.UpdatedBy = userName;
    }

    public static void ApplyResolved(BinRange target, PendingBinConflict c, DateTime now, string userName)
    {
        target.CardSchemeId = c.CardSchemeId;
        target.ProductTypeId = c.ProductTypeId;
        target.FundingTypeId = c.FundingTypeId;
        target.CountryId = c.CountryId;
        target.PrefixLength = c.PrefixLength;
        target.ValidFrom = c.ValidFrom;
        target.ValidTo = c.ValidTo;
        target.IsDeleted = false;
        target.DeletedAt = null;
        target.DeletedBy = null;
        target.UpdatedAt = now;
        target.UpdatedBy = userName;
    }
}
