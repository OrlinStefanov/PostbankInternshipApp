using BinTool.Application.Abstractions;
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
    [InlineData("352800", DetectedScheme.JCB)]        // JCB lower bound
    [InlineData("358900", DetectedScheme.JCB)]        // JCB upper bound
    [InlineData("356000", DetectedScheme.JCB)]
    [InlineData("601100", DetectedScheme.Discover)]   // Discover's most common IIN
    [InlineData("644000", DetectedScheme.Discover)]   // 644-649 lower bound
    [InlineData("649000", DetectedScheme.Discover)]   // 644-649 upper bound
    [InlineData("650000", DetectedScheme.Discover)]
    [InlineData("620000", DetectedScheme.UnionPay)]   // the 62 range, UnionPay's main space
    [InlineData("628800", DetectedScheme.UnionPay)]
    [InlineData("622126", DetectedScheme.UnionPay)]   // Discover co-brand range: UnionPay wins
    [InlineData("810000", DetectedScheme.UnionPay)]   // 810-817 lower bound
    [InlineData("812000", DetectedScheme.UnionPay)]
    [InlineData("814000", DetectedScheme.UnionPay)]
    [InlineData("817000", DetectedScheme.UnionPay)]   // 810-817 upper bound
    public void Known_ranges_are_detected(string prefix, DetectedScheme expected)
    {
        _detector.Detect(prefix).Should().Be(expected);
    }

    [Theory]
    [InlineData("100000")] // 1-series: no scheme
    [InlineData("220000")] // just below the Mastercard 2-series
    [InlineData("280000")] // just above the Mastercard 2-series
    [InlineData("350000")] // just below the JCB range
    [InlineData("352700")] // one below the JCB lower bound
    [InlineData("359000")] // one above the JCB upper bound
    [InlineData("643000")] // one below the Discover 644-649 range
    [InlineData("809000")] // one below the UnionPay 810-817 range
    [InlineData("818000")] // one above the UnionPay 810-817 range
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
    [InlineData(DetectedScheme.JCB, "JCB", true)]
    [InlineData(DetectedScheme.JCB, "jcb", true)]              // case-insensitive
    [InlineData(DetectedScheme.Discover, "Discover", true)]
    [InlineData(DetectedScheme.Discover, "Discover Card", true)] // alias, as in card_schemes.csv
    [InlineData(DetectedScheme.Discover, "Visa", false)]
    [InlineData(DetectedScheme.UnionPay, "UnionPay", true)]
    [InlineData(DetectedScheme.UnionPay, "unionpay", true)]        // case-insensitive
    [InlineData(DetectedScheme.UnionPay, "China UnionPay", true)]  // alias, as in card_schemes.csv
    [InlineData(DetectedScheme.UnionPay, "Discover", false)]
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

    /// <summary>
    /// The name shown to whoever reviews a staged mismatch. Null for
    /// <see cref="DetectedScheme.Unknown"/> is load-bearing: the import service picks its
    /// wording from it, so a detected scheme with no name would report the opposite of
    /// what was found.
    /// </summary>
    [Theory]
    [InlineData(DetectedScheme.Visa, "Visa")]
    [InlineData(DetectedScheme.Mastercard, "Mastercard")]
    [InlineData(DetectedScheme.AmericanExpress, "American Express")]
    [InlineData(DetectedScheme.DinersClub, "Diners Club")]
    [InlineData(DetectedScheme.JCB, "JCB")]
    [InlineData(DetectedScheme.Discover, "Discover")]
    [InlineData(DetectedScheme.UnionPay, "UnionPay")]
    [InlineData(DetectedScheme.Unknown, null)]
    public void DisplayName_names_every_network_it_can_detect(
        DetectedScheme detected, string? expected)
    {
        _detector.DisplayName(detected).Should().Be(expected);
    }
}
