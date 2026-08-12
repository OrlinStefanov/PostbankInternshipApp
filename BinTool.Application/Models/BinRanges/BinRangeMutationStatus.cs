namespace BinTool.Application.Models.BinRanges;

public enum BinRangeMutationStatus
{
    Created = 0,
    Updated = 1,
    Restored = 2,
    Deleted = 3,
    NotFound = 4,
    PrefixInUse = 5,
    Invalid = 6,
    AlreadyInThatState = 7,
    SchemeMismatch = 8
}
