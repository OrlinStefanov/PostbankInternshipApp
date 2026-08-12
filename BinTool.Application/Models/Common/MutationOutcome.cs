namespace BinTool.Application.Models.Common;

public enum MutationOutcome
{
    /// <summary>The write happened.</summary>
    Succeeded,

    /// <summary>The request itself is wrong and will stay wrong until it is changed.</summary>
    Invalid,

    /// <summary>What the request addressed is not there.</summary>
    NotFound,

    /// <summary>
    /// The request is well formed, but the current state refuses it - a name already taken, a rule
    /// that would overlap, the last admin.
    /// </summary>
    Conflict
}

public interface IMutationResult
{
    MutationOutcome Outcome { get; }

    string? Error { get; }
}
