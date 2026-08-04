using BinTool.Infrastructure.Services;
using FluentAssertions;

namespace BinTool.Tests;

/// <summary>
/// Prefix matching, validity filtering and input handling for BIN classification.
/// </summary>
public class BinClassificationServiceTests : SqliteTestBase
{
    private static readonly DateTime Active = new(2024, 1, 1);

    private readonly BinClassificationService _service;

    public BinClassificationServiceTests()
    {
        _service = new BinClassificationService(Db);
    }

    // ---- Matching --------------------------------------------------------------

    [Fact]
    public async Task Classifies_a_bin_with_the_attributes_of_the_matching_range()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);

        var result = await _service.ClassifyAsync("400001");

        result.Matched.Should().BeTrue();
        result.Bin.Should().Be("400001");
        result.MatchedPrefix.Should().Be("400001");
        result.CardScheme.Should().Be("Visa");
        result.ProductType.Should().Be("Consumer");
        result.FundingType.Should().Be("Credit");
        result.CountryCode.Should().Be("US");
        result.CountryName.Should().Be("United States");
        result.ValidFrom.Should().Be(Active);
        result.ValidTo.Should().BeNull();
    }

    [Fact]
    public async Task The_longest_matching_prefix_wins()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);
        SeedBinRange("40000123", VisaId, CommercialId, DebitId, BgCountryId, Active);

        var result = await _service.ClassifyAsync("40000123");

        result.MatchedPrefix.Should().Be("40000123");
        result.ProductType.Should().Be("Commercial");
        result.CountryCode.Should().Be("BG");
    }

    [Fact]
    public async Task Falls_back_to_a_shorter_prefix_when_no_longer_range_exists()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);

        var result = await _service.ClassifyAsync("40000199");

        result.Matched.Should().BeTrue();
        result.MatchedPrefix.Should().Be("400001");
    }

    [Fact]
    public async Task Returns_no_match_when_nothing_covers_the_bin()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);

        var result = await _service.ClassifyAsync("999999");

        result.Matched.Should().BeFalse();
        result.Bin.Should().Be("999999");
        result.MatchedPrefix.Should().BeNull();
        result.CardScheme.Should().BeNull();
        result.Region.Should().BeNull();
    }

    [Theory]
    [InlineData(BgCountryId, "Domestic")]
    [InlineData(DeCountryId, "Intra-EEA")]
    [InlineData(UsCountryId, "Inter-Regional")]
    public async Task Region_is_resolved_through_the_issuing_country(int countryId, string expected)
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, countryId, Active);

        var result = await _service.ClassifyAsync("400001");

        result.Region.Should().Be(expected);
    }

    // ---- Validity and soft delete ----------------------------------------------

    [Fact]
    public async Task An_expired_range_does_not_match()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            Active, validTo: DateTime.UtcNow.Date.AddDays(-1));

        var result = await _service.ClassifyAsync("400001");

        result.Matched.Should().BeFalse();
    }

    [Fact]
    public async Task A_range_that_has_not_started_yet_does_not_match()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            DateTime.UtcNow.Date.AddDays(1));

        var result = await _service.ClassifyAsync("400001");

        result.Matched.Should().BeFalse();
    }

    [Fact]
    public async Task An_open_ended_range_still_matches()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active, validTo: null);

        var result = await _service.ClassifyAsync("400001");

        result.Matched.Should().BeTrue();
    }

    [Fact]
    public async Task A_soft_deleted_range_does_not_match()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);
        range.IsDeleted = true;
        Db.SaveChanges();

        var result = await _service.ClassifyAsync("400001");

        result.Matched.Should().BeFalse();
    }

    [Fact]
    public async Task A_shorter_range_matches_when_the_longer_one_has_expired()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);
        SeedBinRange("40000123", VisaId, CommercialId, DebitId, BgCountryId,
            Active, validTo: DateTime.UtcNow.Date.AddDays(-1));

        var result = await _service.ClassifyAsync("40000123");

        result.MatchedPrefix.Should().Be("400001");
    }

    // ---- Input handling --------------------------------------------------------

    [Fact]
    public async Task A_full_card_number_is_truncated_to_the_lookup_prefix()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);

        var result = await _service.ClassifyAsync("4000019999999991");

        result.Matched.Should().BeTrue();
        result.Bin.Should().Be("40000199", "no more of the card number than the lookup needs is kept");
    }

    [Fact]
    public async Task Surrounding_whitespace_is_ignored()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Active);

        var result = await _service.ClassifyAsync("  400001  ");

        result.Matched.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("40000")]              // shorter than the shortest stored prefix
    [InlineData("4000 01")]            // separators are not accepted
    [InlineData("4000a1")]
    [InlineData("40000123456789012345")] // longer than a PAN
    public async Task Invalid_input_is_rejected(string bin)
    {
        var classify = () => _service.ClassifyAsync(bin);

        await classify.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task The_rejection_message_never_echoes_the_input()
    {
        var classify = () => _service.ClassifyAsync("4000a19999999991");

        var thrown = await classify.Should().ThrowAsync<ArgumentException>();
        thrown.Which.Message.Should().NotContain("4000");
    }
}
