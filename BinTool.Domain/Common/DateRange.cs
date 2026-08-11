namespace BinTool.Domain.Common;

/// <summary>
/// A closed date interval where the end is optional and an absent end means "open-ended".
/// <para>
/// Both ends are inclusive. Written out by hand, the overlap test is
/// <c>a.From &lt;= b.To &amp;&amp; b.From &lt;= a.To</c> with a null end standing in for infinity - short
/// enough to retype and just subtle enough to get wrong, which is why it lives here once
/// instead of at every call site that needs it.
/// </para>
/// </summary>
public readonly record struct DateRange(DateTime From, DateTime? To)
{
    /// <summary>An absent end treated as infinity, so comparisons need no null branch.</summary>
    public DateTime EffectiveTo => To ?? DateTime.MaxValue;

    /// <summary>
    /// A range covering whole days: both ends truncated to midnight. Validity is decided per
    /// day, so a time component would make a rule's last day depend on the clock.
    /// </summary>
    public static DateRange OfDays(DateTime from, DateTime? to) => new(from.Date, to?.Date);

    /// <summary>True when the two ranges share at least one day.</summary>
    public bool Overlaps(DateRange other) =>
        From <= other.EffectiveTo && other.From <= EffectiveTo;

    /// <summary>True when the date falls inside the range, both ends inclusive.</summary>
    public bool Contains(DateTime date) => date >= From && date <= EffectiveTo;

    /// <summary>True when the date is before the range starts.</summary>
    public bool StartsAfter(DateTime date) => From > date;

    /// <summary>True when the range has already closed by the given date.</summary>
    public bool EndedBefore(DateTime date) => To is { } to && to < date;

    /// <summary>False when the end precedes the start, which is not a range at all.</summary>
    public bool IsWellFormed => To is null || To >= From;
}
