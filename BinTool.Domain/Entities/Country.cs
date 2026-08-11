namespace BinTool.Domain.Entities;

public class Country
{
    public int CountryId { get; set; }

    public string IsoCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int RegionId { get; set; }

    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? UpdatedBy { get; set; }

    #endregion

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    public Region? Region { get; set; }

    public ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();

    #endregion
}
