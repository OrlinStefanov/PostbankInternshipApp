namespace BinTool.Core.Entities;

/// <summary>
/// Types of changes that can be audited
/// </summary>
public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Deactivated = 4,
    Imported = 5,
}
