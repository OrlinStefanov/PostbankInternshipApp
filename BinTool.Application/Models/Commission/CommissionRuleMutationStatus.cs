namespace BinTool.Application.Models.Commission;

public enum CommissionRuleMutationStatus
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Restored = 3,
    NotFound = 4,
    Invalid = 5,
    InUse = 6,
    AlreadyInThatState = 7,
    Overlap = 8
}
