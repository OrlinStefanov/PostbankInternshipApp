namespace BinTool.Application.Models.ReferenceData;

public class CountryListItem
{
    public int Id { get; set; }

    public string IsoCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int RegionId { get; set; }

    public string RegionName { get; set; } = string.Empty;

    public LookupStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
