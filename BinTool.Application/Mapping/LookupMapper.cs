using BinTool.Application.Models.Audit;
using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Mapping;

public static class LookupMapper
{
    public static string NormalizedName(this LookupInput input) => input.Name.Trim();

    // A blank description is stored as null rather than "", so "no description" has one
    // representation instead of two that render the same but do not compare equal.
    public static string? NormalizedDescription(this LookupInput input) =>
        string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();

    public static void Apply(ILookupEntity entity, LookupInput input)
    {
        entity.Name = input.NormalizedName();
        entity.Description = input.NormalizedDescription();
    }

    public static LookupSnapshot ToSnapshot(ILookupEntity entity) =>
        new(entity.Name, entity.Description, entity.IsDeleted);

    public static LookupListItem ToListItem(ILookupEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Status = entity.IsDeleted ? LookupStatus.Deleted : LookupStatus.Active,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static string EntityTypeFor(LookupKind kind) => kind switch
    {
        LookupKind.CardScheme => AuditEntityTypes.CardScheme,
        LookupKind.ProductType => AuditEntityTypes.ProductType,
        LookupKind.FundingType => AuditEntityTypes.FundingType,
        LookupKind.Region => AuditEntityTypes.Region,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown lookup kind.")
    };

    public static string DisplayName(LookupKind kind) => kind switch
    {
        LookupKind.CardScheme => "Card scheme",
        LookupKind.ProductType => "Product type",
        LookupKind.FundingType => "Funding type",
        LookupKind.Region => "Region",
        _ => kind.ToString()
    };

    /// <summary>What a lookup row of this kind is pointed at by, for the in-use refusal.</summary>
    public static string ReferenceDescription(LookupKind kind) =>
        kind == LookupKind.Region ? "country/countries" : "BIN range(s)";
}
