namespace BinTool.Core.Models.Audit;

/// <summary>
/// What a currency looked like at one moment, as stored in an audit entry's
/// <c>OldValues</c> / <c>NewValues</c>.
/// </summary>
public sealed record CurrencySnapshot(
    string Code,
    string Name,
    decimal RateToEur,
    bool IsActive,
    bool IsDeleted);
