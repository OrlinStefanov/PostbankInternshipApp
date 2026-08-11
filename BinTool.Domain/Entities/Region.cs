namespace BinTool.Domain.Entities;

public class Region : ILookupEntity
{
    public int RegionId { get; set; }

    int ILookupEntity.Id => RegionId;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    public ICollection<Country> Countries { get; set; } = new List<Country>();

    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    #endregion
}
