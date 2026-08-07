using BinTool.Core.Services;
using BinTool.Infrastructure.Services;
using FluentAssertions;

namespace BinTool.Tests;

/// <summary>
/// The prefix-to-network rules used to catch a mislabelled import. Pure logic over the
/// published IIN ranges, so these run without a database.
/// </summary>
public class CardSchemeDetectorTests
{
    private readonly CardSchemeDetector _detector = new();

    [Theory]
    [InlineData("400001", DetectedScheme.Visa)]
    [InlineData("4", DetectedScheme.Visa)]
    [InlineData("510000", DetectedScheme.Mastercard)]
    [InlineData("550000", DetectedScheme.Mastercard)]
    [InlineData("222100", DetectedScheme.Mastercard)] // 2-series lower bound
    [InlineData("272000", DetectedScheme.Mastercard)] // 2-series upper bound
    [InlineData("340000", DetectedScheme.AmericanExpress)]
    [InlineData("370000", DetectedScheme.AmericanExpress)]
    [InlineData("300000", DetectedScheme.DinersClub)]
    [InlineData("360000", DetectedScheme.DinersClub)]
    [InlineData("309500", DetectedScheme.DinersClub)]
    public void Known_ranges_are_detected(string prefix, DetectedScheme expected)
    {
        _detector.Detect(prefix).Should().Be(expected);
    }

    [Theory]
    [InlineData("100000")] // 1-series: no scheme
    [InlineData("220000")] // just below the Mastercard 2-series
    [InlineData("280000")] // just above the Mastercard 2-series
    [InlineData("350000")] // JCB - not a scheme this detector knows
    [InlineData("620000")] // UnionPay - not known
    [InlineData("12A456")] // not all digits
    [InlineData("")]
    public void Unknown_ranges_report_unknown(string prefix)
    {
        _detector.Detect(prefix).Should().Be(DetectedScheme.Unknown);
    }

    [Theory]
    [InlineData(DetectedScheme.Visa, "Visa", true)]
    [InlineData(DetectedScheme.Visa, "visa", true)]            // case-insensitive
    [InlineData(DetectedScheme.Visa, "Mastercard", false)]
    [InlineData(DetectedScheme.Mastercard, "Mastercard", true)]
    [InlineData(DetectedScheme.Mastercard, "Maestro", true)]   // alias
    [InlineData(DetectedScheme.AmericanExpress, "American Express", true)]
    [InlineData(DetectedScheme.AmericanExpress, "Amex", true)] // alias
    [InlineData(DetectedScheme.DinersClub, "Diners Club", true)]
    [InlineData(DetectedScheme.Unknown, "Visa", false)]        // unknown matches nothing
    public void Matches_compares_a_declared_name_to_the_detected_network(
        DetectedScheme detected, string declared, bool expected)
    {
        _detector.Matches(detected, declared).Should().Be(expected);
    }

    [Fact]
    public void Matches_is_false_for_a_blank_declared_name()
    {
        _detector.Matches(DetectedScheme.Visa, null).Should().BeFalse();
        _detector.Matches(DetectedScheme.Visa, "  ").Should().BeFalse();
    }
}
