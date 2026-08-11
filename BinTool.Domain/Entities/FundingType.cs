namespace BinTool.Domain.Entities;

public class FundingType : ILookupEntity
{
    public int FundingTypeId { get; set; }

    int ILookupEntity.Id => FundingTypeId;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    public ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();

    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    #endregion
}
