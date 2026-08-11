using BinTool.Application.Abstractions;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Services;
using BinTool.Infrastructure.Repositories;
using FluentAssertions;

namespace BinTool.Tests;

public class BinRangeQueryServiceMismatchTests : SqliteTestBase
{
    private static readonly DateTime Started = new(2024, 1, 1);

    private readonly BinRangeQueryService _service;

    public BinRangeQueryServiceMismatchTests()
    {
        _service = new BinRangeQueryService(new BinRangeRepository(Db), new CardSchemeDetector());
    }

    [Fact]
    public async Task A_row_whose_stored_scheme_matches_the_detector_is_not_a_vulnerability()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.GetSchemeMismatchesAsync(1, 25);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task A_row_whose_stored_scheme_contradicts_the_detector_is_surfaced_with_the_detected_name()
    {
        // A Visa prefix stored under Mastercard - exactly the scenario an admin needs to see.
        SeedBinRange("400001", MastercardId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.GetSchemeMismatchesAsync(1, 25);

        var item = result.Items.Should().ContainSingle().Subject;
        item.Prefix.Should().Be("400001");
        item.CardScheme.Should().Be("Mastercard");
        item.DetectedScheme.Should().Be("Visa");
    }

    [Fact]
    public async Task A_row_the_detector_cannot_judge_is_not_a_vulnerability()
    {
        // 999999 sits in no known IIN range, so the detector cannot say anything about it -
        // absence of a verdict is not the same as a contradiction.
        SeedBinRange("999999", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.GetSchemeMismatchesAsync(1, 25);

        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task A_soft_deleted_row_is_not_a_vulnerability_even_when_its_scheme_is_wrong()
    {
        var range = SeedBinRange("400001", MastercardId, ConsumerId, CreditId, UsCountryId, Started);
        range.IsDeleted = true;
        Db.SaveChanges();

        var result = await _service.GetSchemeMismatchesAsync(1, 25);

        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Paging_slices_the_mismatch_set_and_carries_the_full_total()
    {
        // Three Visa prefixes all stored under Mastercard, so every one is a mismatch.
        SeedBinRange("400001", MastercardId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400002", MastercardId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400003", MastercardId, ConsumerId, CreditId, UsCountryId, Started);

        var pageOne = await _service.GetSchemeMismatchesAsync(1, 2);
        pageOne.TotalCount.Should().Be(3);
        pageOne.Items.Should().HaveCount(2);
        pageOne.Items.Select(i => i.Prefix).Should().ContainInOrder("400001", "400002");
        pageOne.HasNext.Should().BeTrue();

        var pageTwo = await _service.GetSchemeMismatchesAsync(2, 2);
        pageTwo.TotalCount.Should().Be(3);
        pageTwo.Items.Should().ContainSingle().Which.Prefix.Should().Be("400003");
        pageTwo.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task Count_matches_the_paged_total()
    {
        SeedBinRange("400001", MastercardId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400002", MastercardId, ConsumerId, CreditId, UsCountryId, Started);
        // A clean row is not counted.
        SeedBinRange("520000", MastercardId, ConsumerId, DebitId, BgCountryId, Started);

        var count = await _service.CountSchemeMismatchesAsync();
        var paged = await _service.GetSchemeMismatchesAsync(1, 25);

        count.Should().Be(2);
        count.Should().Be(paged.TotalCount);
    }
}
