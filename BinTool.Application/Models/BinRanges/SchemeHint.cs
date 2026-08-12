namespace BinTool.Application.Models.BinRanges;

/// <summary>
/// What the card-scheme detector makes of a prefix, for a client editing one.
/// </summary>
/// <param name="DetectedScheme">
/// The network the prefix's digits belong to, or null when it falls outside every published
/// range the detector knows. Null is an absence of an opinion, not a fault.
/// </param>
/// <param name="MatchesDeclared">
/// Whether the declared scheme is the network detected. False whenever nothing was declared, and
/// always false when <paramref name="DetectedScheme"/> is null - there is nothing to agree with.
/// Answered here rather than by comparing names on the client, because the detector accepts
/// aliases: reference data naming a scheme "Amex" agrees with a detected "American Express".
/// </param>
public sealed record SchemeHint(string? DetectedScheme, bool MatchesDeclared);
