using BinTool.Domain.Common;
using FluentAssertions;

namespace BinTool.Tests;

// The validity-window arithmetic every rule check leans on. No database and no fixtures - the whole
// point of moving this into a value object was that it could be tested like this.
public class DateRangeTests
{
    private static DateTime D(int day) => new(2025, 6, day);

    // ---- Overlap ----------------------------------------------------------------

    [Fact]
    public void Ranges_that_share_days_overlap()
    {
        new DateRange(D(1), D(10)).Overlaps(new DateRange(D(5), D(15)))
            .Should().BeTrue();
    }

    [Fact]
    public void Ranges_that_share_nothing_do_not_overlap()
    {
        new DateRange(D(1), D(10)).Overlaps(new DateRange(D(11), D(20)))
            .Should().BeFalse();
    }

    [Fact]
    public void Touching_at_a_single_day_counts_as_overlap()
    {
        // Both ends are inclusive, so a rule ending on the 10th and one starting on the 10th
        // are both in force that day - which is exactly the case a save has to refuse.
        new DateRange(D(1), D(10)).Overlaps(new DateRange(D(10), D(20)))
            .Should().BeTrue();
    }

    [Fact]
    public void Overlap_is_symmetric()
    {
        var a = new DateRange(D(1), D(10));
        var b = new DateRange(D(5), null);

        a.Overlaps(b).Should().Be(b.Overlaps(a));
    }

    [Fact]
    public void An_open_ended_range_overlaps_everything_that_starts_after_it()
    {
        new DateRange(D(1), null).Overlaps(new DateRange(D(20), D(25)))
            .Should().BeTrue();
    }

    [Fact]
    public void An_open_ended_range_does_not_overlap_what_ended_before_it_began()
    {
        new DateRange(D(20), null).Overlaps(new DateRange(D(1), D(10)))
            .Should().BeFalse();
    }

    // ---- Containment ------------------------------------------------------------

    [Theory]
    [InlineData(1, true)]   // first day, inclusive
    [InlineData(5, true)]
    [InlineData(10, true)]  // last day, inclusive
    [InlineData(11, false)]
    public void Contains_includes_both_ends(int day, bool expected)
    {
        new DateRange(D(1), D(10)).Contains(D(day)).Should().Be(expected);
    }

    [Fact]
    public void An_open_ended_range_contains_any_later_date()
    {
        new DateRange(D(1), null).Contains(new DateTime(2099, 1, 1)).Should().BeTrue();
    }

    // ---- Lifecycle questions ----------------------------------------------------

    [Fact]
    public void A_range_starting_later_starts_after_today()
    {
        new DateRange(D(20), null).StartsAfter(D(1)).Should().BeTrue();
    }

    [Fact]
    public void A_closed_range_has_ended_before_a_later_date()
    {
        new DateRange(D(1), D(10)).EndedBefore(D(11)).Should().BeTrue();
    }

    [Fact]
    public void An_open_ended_range_never_ends()
    {
        new DateRange(D(1), null).EndedBefore(new DateTime(2099, 1, 1)).Should().BeFalse();
    }

    // ---- Well-formedness --------------------------------------------------------

    [Fact]
    public void An_end_before_the_start_is_not_well_formed()
    {
        new DateRange(D(10), D(1)).IsWellFormed.Should().BeFalse();
    }

    [Fact]
    public void An_end_equal_to_the_start_is_a_single_day_and_well_formed()
    {
        new DateRange(D(10), D(10)).IsWellFormed.Should().BeTrue();
    }

    [Fact]
    public void An_open_ended_range_is_always_well_formed()
    {
        new DateRange(D(10), null).IsWellFormed.Should().BeTrue();
    }

    // ---- Whole days -------------------------------------------------------------

    [Fact]
    public void OfDays_drops_the_time_from_both_ends()
    {
        var range = DateRange.OfDays(
            new DateTime(2025, 6, 1, 23, 59, 59),
            new DateTime(2025, 6, 10, 0, 0, 1));

        range.From.Should().Be(D(1));
        range.To.Should().Be(D(10));
    }

    [Fact]
    public void OfDays_keeps_an_open_end_open()
    {
        DateRange.OfDays(new DateTime(2025, 6, 1, 12, 0, 0), null).To.Should().BeNull();
    }
}
