namespace BinTool.Domain.Entities;

public interface ILookupEntity
{
    int Id { get; }

    string Name { get; set; }

    string? Description { get; set; }

    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }

    string? DeletedBy { get; set; }
}
