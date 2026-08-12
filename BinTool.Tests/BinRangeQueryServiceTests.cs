using BinTool.Application.Abstractions;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Services;
using BinTool.Domain.Entities;
using BinTool.Infrastructure.Repositories;
using FluentAssertions;

namespace BinTool.Tests;

public class BinRangeQueryServiceTests : SqliteTestBase
{
    private static readonly DateTime Started = new(2024, 1, 1);
    private static readonly DateTime Today = DateTime.UtcNow.Date;

    private readonly BinRangeQueryService _service;

    public BinRangeQueryServiceTests()
    {
        _service = new BinRangeQueryService(new BinRangeRepository(Db), new CardSchemeDetector());
    }

    private Task<PagedResult<BinRangeListItem>> Search(BinRangeQuery? query = null) =>
        _service.SearchAsync(query ?? new BinRangeQuery());

    // ---- Filtering -------------------------------------------------------------

    [Fact]
    public async Task Lists_every_stored_range_when_no_filter_is_given()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("520000", MastercardId, ConsumerId, DebitId, BgCountryId, Started);

        var result = await Search();

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Results_are_ordered_by_prefix()
    {
        SeedBinRange("520000", MastercardId, ConsumerId, DebitId, BgCountryId, Started);
        SeedBinRange("340000", AmexId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Search();

        result.Items.Select(i => i.Prefix).Should().ContainInOrder("340000", "400001", "520000");
    }

    [Fact]
    public async Task Prefix_filter_matches_the_start_of_the_prefix()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("40000123", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("520000", MastercardId, ConsumerId, DebitId, BgCountryId, Started);

        var result = await Search(new BinRangeQuery { Prefix = "4000" });

        result.Items.Select(i => i.Prefix).Should().BeEquivalentTo("400001", "40000123");
    }

    [Theory]
    [InlineData("Visa")]
    [InlineData("visa")]
    [InlineData("VISA")]
    public async Task Name_filters_are_case_insensitive(string scheme)
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("520000", MastercardId, ConsumerId, DebitId, BgCountryId, Started);

        var result = await Search(new BinRangeQuery { CardScheme = scheme });

        result.Items.Should().ContainSingle().Which.Prefix.Should().Be("400001");
    }

    [Fact]
    public async Task Country_is_filtered_by_iso_code()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400002", VisaId, ConsumerId, CreditId, BgCountryId, Started);

        var result = await Search(new BinRangeQuery { CountryCode = "bg" });

        result.Items.Should().ContainSingle().Which.CountryName.Should().Be("Bulgaria");
    }

    [Fact]
    public async Task Filters_combine_with_and()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400002", VisaId, CommercialId, CreditId, UsCountryId, Started);
        SeedBinRange("520000", MastercardId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Search(new BinRangeQuery
        {
            CardScheme = "Visa",
            ProductType = "Consumer",
            CountryCode = "US"
        });

        result.Items.Should().ContainSingle().Which.Prefix.Should().Be("400001");
    }

    // ---- Derived status --------------------------------------------------------

    [Fact]
    public async Task Status_is_derived_from_the_dates_and_the_delete_flag()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400002", VisaId, ConsumerId, CreditId, UsCountryId,
            Started, validTo: Today.AddDays(-1));
        SeedBinRange("400003", VisaId, ConsumerId, CreditId, UsCountryId, Today.AddDays(1));

        var deleted = SeedBinRange("400004", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        deleted.IsDeleted = true;
        Db.SaveChanges();

        var live = await Search();
        live.Items.Should().Contain(i => i.Prefix == "400001" && i.Status == BinRangeStatus.Active);
        live.Items.Should().Contain(i => i.Prefix == "400002" && i.Status == BinRangeStatus.Expired);
        live.Items.Should().Contain(i => i.Prefix == "400003" && i.Status == BinRangeStatus.Scheduled);

        var removed = await Search(new BinRangeQuery { Status = BinRangeStatus.Deleted });
        removed.Items.Should().ContainSingle()
            .Which.Status.Should().Be(BinRangeStatus.Deleted);
    }

    [Fact]
    public async Task Deleted_ranges_are_excluded_unless_asked_for()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        var deleted = SeedBinRange("400002", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        deleted.IsDeleted = true;
        Db.SaveChanges();

        var result = await Search();

        result.Items.Should().ContainSingle().Which.Prefix.Should().Be("400001");
    }

    [Theory]
    [InlineData(BinRangeStatus.Active, "400001")]
    [InlineData(BinRangeStatus.Expired, "400002")]
    [InlineData(BinRangeStatus.Scheduled, "400003")]
    public async Task Filtering_by_status_returns_only_that_status(
        BinRangeStatus status, string expectedPrefix)
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("400002", VisaId, ConsumerId, CreditId, UsCountryId,
            Started, validTo: Today.AddDays(-1));
        SeedBinRange("400003", VisaId, ConsumerId, CreditId, UsCountryId, Today.AddDays(1));

        var result = await Search(new BinRangeQuery { Status = status });

        result.Items.Should().ContainSingle().Which.Prefix.Should().Be(expectedPrefix);
    }

    [Fact]
    public async Task A_range_ending_today_is_still_active()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started, validTo: Today);

        var result = await Search(new BinRangeQuery { Status = BinRangeStatus.Active });

        result.Items.Should().ContainSingle();
    }

    // ---- Paging ----------------------------------------------------------------

    [Fact]
    public async Task Paging_returns_the_requested_slice_and_the_full_match_count()
    {
        for (var i = 0; i < 12; i++)
            SeedBinRange($"4000{i:D2}", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Search(new BinRangeQuery { Page = 2, PageSize = 5 });

        result.Items.Should().HaveCount(5);
        result.Items.First().Prefix.Should().Be("400005");
        result.TotalCount.Should().Be(12);
        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task A_page_past_the_end_is_empty_but_still_reports_the_totals()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Search(new BinRangeQuery { Page = 5, PageSize = 25 });

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(1);
        result.HasNext.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, BinRangeQuery.DefaultPageSize)]
    [InlineData(-10, BinRangeQuery.DefaultPageSize)]
    [InlineData(5000, BinRangeQuery.MaxPageSize)]
    public async Task Page_size_is_clamped(int requested, int expected)
    {
        var result = await Search(new BinRangeQuery { PageSize = requested });

        result.PageSize.Should().Be(expected);
    }

    [Fact]
    public async Task A_page_number_below_one_is_treated_as_the_first_page()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Search(new BinRangeQuery { Page = 0 });

        result.Page.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Nothing_matching_returns_an_empty_page_rather_than_failing()
    {
        var result = await Search(new BinRangeQuery { Prefix = "999999" });

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    // ---- Detected-scheme flag on the browse listing ----------------------------

    [Fact]
    public async Task Browse_flags_a_row_whose_stored_scheme_contradicts_the_detector()
    {
        // A Visa prefix stored under Mastercard: the browse listing carries the
        // detector's opinion so a viewer sees the disagreement without having to open
        // the scheme-mismatch page.
        SeedBinRange("400001", MastercardId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Search();

        result.Items.Should().ContainSingle().Which.DetectedScheme.Should().Be("Visa");
    }

    [Fact]
    public async Task Browse_leaves_detected_scheme_null_when_the_row_is_consistent()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Search();

        result.Items.Should().ContainSingle().Which.DetectedScheme.Should().BeNull();
    }

    // ---- Attribution -----------------------------------------------------------

    [Fact]
    public async Task Each_row_reports_who_added_it_and_who_last_changed_it()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        range.CreatedBy = "admin";
        range.UpdatedBy = "someone-else";
        Db.SaveChanges();

        var result = await Search();

        var item = result.Items.Should().ContainSingle().Subject;
        item.CreatedBy.Should().Be("admin");
        item.UpdatedBy.Should().Be("someone-else");
        item.CreatedAt.Should().BeCloseTo(range.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task A_row_written_without_a_signed_in_user_is_attributed_to_system()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        range.CreatedBy = "system";
        range.UpdatedBy = "system";
        Db.SaveChanges();

        var result = await Search();

        result.Items.Should().ContainSingle().Which.CreatedBy.Should().Be("system");
    }

    // ---- Added-by filter -------------------------------------------------------

    [Theory]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    public async Task Added_by_filter_returns_only_that_creators_ranges(string filter)
    {
        SeedBinRangeAddedBy("400001", "admin");
        SeedBinRangeAddedBy("520000", "viewer");

        var result = await Search(new BinRangeQuery { CreatedBy = filter });

        result.Items.Should().ContainSingle().Which.Prefix.Should().Be("400001");
    }

    [Fact]
    public async Task Added_by_can_reach_a_deleted_range_when_deleted_is_also_asked_for()
    {
        var deleted = SeedBinRangeAddedBy("400001", "admin");
        deleted.IsDeleted = true;
        Db.SaveChanges();

        var result = await Search(new BinRangeQuery
        {
            CreatedBy = "admin",
            Status = BinRangeStatus.Deleted
        });

        result.Items.Should().ContainSingle().Which.Prefix.Should().Be("400001");
    }

    // ---- Filter options --------------------------------------------------------

    [Fact]
    public async Task Filter_options_come_from_the_seeded_reference_data()
    {
        var options = await _service.GetFilterOptionsAsync();

        options.CardSchemes.Should().Contain(new[] { "Visa", "Mastercard", "American Express" });
        options.ProductTypes.Should().Contain(new[] { "Consumer", "Commercial", "Prepaid" });
        options.FundingTypes.Should().Contain(new[] { "Credit", "Debit" });
        options.Countries.Should().Contain(c => c.IsoCode == "BG" && c.Name == "Bulgaria");
    }

    [Fact]
    public async Task Creators_option_lists_distinct_names_deleted_rows_included()
    {
        SeedBinRangeAddedBy("400001", "admin");
        SeedBinRangeAddedBy("400002", "admin");
        var deleted = SeedBinRangeAddedBy("520000", "viewer");
        deleted.IsDeleted = true;
        Db.SaveChanges();

        var options = await _service.GetFilterOptionsAsync();

        // Distinct, sorted, and the deleted row's creator is still offered.
        options.Creators.Should().Equal("admin", "viewer");
    }

    private BinRange SeedBinRangeAddedBy(string prefix, string createdBy)
    {
        var range = SeedBinRange(prefix, VisaId, ConsumerId, CreditId, UsCountryId, Started);
        range.CreatedBy = createdBy;
        Db.SaveChanges();
        return range;
    }
}
