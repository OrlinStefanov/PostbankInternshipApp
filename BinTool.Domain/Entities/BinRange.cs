namespace BinTool.Domain.Entities;

public class BinRange
{
    public int BinRangeId { get; set; }

    public string Prefix { get; set; } = string.Empty;

    public int PrefixLength { get; set; }

    public int CardSchemeId { get; set; }

    public int ProductTypeId { get; set; }

    public int FundingTypeId { get; set; }

    public int CountryId { get; set; }

    // Both ends inclusive; a null ValidTo runs forever. DateRange holds the arithmetic.
    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    #endregion

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    public CardScheme? CardScheme { get; set; }

    public ProductType? ProductType { get; set; }

    public FundingType? FundingType { get; set; }

    public Country? Country { get; set; }

    #endregion
}
