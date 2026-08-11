namespace BinTool.Domain.Common;

/// <summary>
/// A closed date interval where the end is optional and an absent end means "open-ended".
/// </summary>
public readonly record struct DateRange(DateTime From, DateTime? To)
{
    public DateTime EffectiveTo => To ?? DateTime.MaxValue;

    public static DateRange OfDays(DateTime from, DateTime? to) => new(from.Date, to?.Date);

    public bool Overlaps(DateRange other) =>
        From <= other.EffectiveTo && other.From <= EffectiveTo;

    public bool Contains(DateTime date) => date >= From && date <= EffectiveTo;

    public bool StartsAfter(DateTime date) => From > date;

    public bool EndedBefore(DateTime date) => To is { } to && to < date;

    public bool IsWellFormed => To is null || To >= From;
}
