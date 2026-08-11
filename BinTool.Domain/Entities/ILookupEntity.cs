namespace BinTool.Domain.Entities;

/// <summary>
/// The shape the four Name+Description reference tables share: card scheme, product type,
/// funding type and region. They differ only in the name of their key column, which is why
/// one service and one repository can serve all four.
/// <para>
/// <see cref="Id"/> is implemented explicitly on each entity so it stays a read-only view
/// over the real key property and Entity Framework does not try to map it as a column.
/// </para>
/// </summary>
public interface ILookupEntity
{
    int Id { get; }

    string Name { get; set; }

    string? Description { get; set; }

    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }

    string? DeletedBy { get; set; }
}
