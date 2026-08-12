using BinTool.Application.Services;
using BinTool.Infrastructure.Repositories;
using FluentAssertions;

namespace BinTool.Tests;

// The hint the BIN range editor draws its scheme suggestion from. It reads nothing, so these
// exercise the detector's two answers as the editor consumes them - which network, and whether
// the scheme the user picked is that network.
public class SchemeHintTests : SqliteTestBase
{
    private readonly BinRangeQueryService _service;

    public SchemeHintTests()
    {
        _service = new BinRangeQueryService(new BinRangeRepository(Db), new CardSchemeDetector());
    }

    [Theory]
    [InlineData("400001", "Visa")]
    [InlineData("520001", "Mastercard")]
    [InlineData("340003", "American Express")]
    public void A_known_prefix_reports_its_network(string prefix, string expected)
    {
        _service.DetectScheme(prefix, declaredScheme: null).DetectedScheme.Should().Be(expected);
    }

    [Fact]
    public void A_prefix_in_no_published_range_reports_no_opinion()
    {
        var hint = _service.DetectScheme("999999", declaredScheme: "Visa");

        hint.DetectedScheme.Should().BeNull();

        // Nothing was detected, so there is nothing for the declared scheme to agree with. The
        // editor shows "no known range" rather than accusing the user of a mismatch.
        hint.MatchesDeclared.Should().BeFalse();
    }

    [Fact]
    public void The_declared_scheme_agrees_when_it_is_the_detected_network()
    {
        _service.DetectScheme("520001", "Mastercard").MatchesDeclared.Should().BeTrue();
    }

    [Fact]
    public void The_declared_scheme_disagrees_when_it_is_a_different_network()
    {
        _service.DetectScheme("520001", "Visa").MatchesDeclared.Should().BeFalse();
    }

    [Fact]
    public void An_alias_of_the_detected_network_still_agrees()
    {
        // The whole reason agreement is decided on the server: reference data may name the
        // scheme "Amex" while the detector's display name is "American Express". Comparing the
        // two strings in the browser would report a mismatch that is not one.
        var hint = _service.DetectScheme("340003", "Amex");

        hint.DetectedScheme.Should().Be("American Express");
        hint.MatchesDeclared.Should().BeTrue();
    }

    [Fact]
    public void Nothing_declared_yet_is_not_agreement()
    {
        // The editor uses this to tell "here is a suggestion" from "the scheme you chose is
        // right", which are different messages under the select.
        _service.DetectScheme("400001", declaredScheme: null).MatchesDeclared.Should().BeFalse();
    }

    [Fact]
    public void A_full_card_number_is_truncated_rather_than_refused()
    {
        // The editor sends whatever is in the prefix box, and the same normalization the range
        // endpoints use applies - so a pasted PAN gives a hint instead of an error.
        _service.DetectScheme("4000011122223333", null).DetectedScheme.Should().Be("Visa");
    }

    [Theory]
    [InlineData("")]
    [InlineData("40000")]
    [InlineData("4000a1")]
    [InlineData("40000111222233334444")]
    public void An_unusable_prefix_is_refused_rather_than_guessed_at(string prefix)
    {
        var refuse = () => _service.DetectScheme(prefix, null);

        refuse.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_refusal_never_echoes_the_value_it_refused()
    {
        // The prefix box may hold a pasted card number, so the message that comes back - and
        // ends up in a 400 body and a log line - must not carry it.
        const string pan = "40000111222233339999";

        var refuse = () => _service.DetectScheme(pan, null);

        refuse.Should().Throw<ArgumentException>()
            .Which.Message.Should().NotContain(pan);
    }
}
