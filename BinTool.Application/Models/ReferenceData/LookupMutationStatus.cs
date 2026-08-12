namespace BinTool.Application.Models.ReferenceData;

public enum LookupMutationStatus
{
    Created = 0,
    Updated = 1,
    Restored = 2,
    Deleted = 3,
    NotFound = 4,
    NameInUse = 5,
    Invalid = 6,
    AlreadyInThatState = 7,
    InUse = 8
}
