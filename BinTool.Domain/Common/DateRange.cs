namespace BinTool.Domain.Common;

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
