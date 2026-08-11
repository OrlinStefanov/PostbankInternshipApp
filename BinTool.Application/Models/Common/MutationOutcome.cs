namespace BinTool.Application.Models.Common;

/// <summary>
/// What a write amounted to, in the four kinds a caller can act on. Every feature has its own
/// status enum saying <em>which</em> rule refused - <c>NameInUse</c>, <c>Overlap</c>,
/// <c>LastAdmin</c> - and those stay, because that is what the message on screen is built
/// from. This says what a client should <em>do</em> about it, which is the only part the
/// transport needs to know.
/// </summary>
public enum MutationOutcome
{
    /// <summary>The write happened.</summary>
    Succeeded,

    /// <summary>The request itself is wrong and will stay wrong until it is changed.</summary>
    Invalid,

    /// <summary>What the request addressed is not there.</summary>
    NotFound,

    /// <summary>
    /// The request is well formed, but the current state refuses it - a name already taken,
    /// a rule that would overlap, the last admin. Retrying unchanged will refuse again;
    /// changing the world may not.
    /// </summary>
    Conflict
}

/// <summary>
/// The part of a write result the API layer reads. Everything else on a result - which item
/// was written, which rule conflicted - belongs to the feature, and the API passes it through
/// untouched as the response body.
/// </summary>
public interface IMutationResult
{
    MutationOutcome Outcome { get; }

    string? Error { get; }
}
