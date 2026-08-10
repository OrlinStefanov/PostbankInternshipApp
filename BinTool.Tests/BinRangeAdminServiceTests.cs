using BinTool.Core.Models.BinRanges;
using BinTool.Core.Services;
using BinTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

/// <summary>
/// Adding, editing, deleting and restoring one BIN range by hand.
/// </summary>
public class BinRangeAdminServiceTests : SqliteTestBase
{
    private static readonly DateTime Started = new(2024, 1, 1);
    private static readonly DateTime Today = DateTime.UtcNow.Date;

    private const string AdminUserId = "admin-user-id";

    private readonly BinRangeAdminService _service;

    public BinRangeAdminServiceTests()
    {
        SeedUser(AdminUserId, "admin");

        var currentUser = new TestUser(AdminUserId, "admin");
        _service = new BinRangeAdminService(
            Db, currentUser, new AuditLog(Db, currentUser), new CardSchemeDetector());
    }

    private static BinRangeInput Input(
        string prefix = "400001", string cardScheme = "Visa", string productType = "Consumer",
        string fundingType = "Credit", string countryCode = "US",
        DateTime? validFrom = null, DateTime? validTo = null,
        bool acknowledgeSchemeMismatch = false) => new()
    {
        Prefix = prefix,
        CardScheme = cardScheme,
        ProductType = productType,
        FundingType = fundingType,
        CountryCode = countryCode,
        ValidFrom = validFrom ?? Started,
        ValidTo = validTo,
        AcknowledgeSchemeMismatch = acknowledgeSchemeMismatch
    };

    // ---- Create ----------------------------------------------------------------

    [Fact]
    public async Task Adding_a_range_stores_it_and_returns_it_as_the_listing_would()
    {
        var result = await _service.CreateAsync(Input(validTo: new DateTime(2030, 1, 1)));

        result.Status.Should().Be(BinRangeMutationStatus.Created);
        result.Succeeded.Should().BeTrue();

        var range = result.Range!;
        range.Prefix.Should().Be("400001");
        range.PrefixLength.Should().Be(6);
        range.CardScheme.Should().Be("Visa");
        range.CountryName.Should().Be("United States");
        range.Region.Should().NotBeEmpty();
        range.Status.Should().Be(BinRangeStatus.Active);

        var stored = await NewContext().BinRanges.SingleAsync();
        stored.Prefix.Should().Be("400001");
        stored.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Whoever_added_the_range_is_recorded_on_it()
    {
        var result = await _service.CreateAsync(Input());

        result.Range!.CreatedBy.Should().Be("admin");
        result.Range.UpdatedBy.Should().Be("admin");
    }

    [Theory]
    [InlineData("visa", "consumer", "credit", "us")]
    [InlineData("VISA", "CONSUMER", "CREDIT", "US")]
    public async Task Reference_data_is_named_case_insensitively(
        string scheme, string product, string funding, string country)
    {
        var result = await _service.CreateAsync(
            Input(cardScheme: scheme, productType: product, fundingType: funding, countryCode: country));

        result.Status.Should().Be(BinRangeMutationStatus.Created);
        result.Range!.CardScheme.Should().Be("Visa");
    }

    [Fact]
    public async Task Adding_a_prefix_that_already_exists_is_refused_without_touching_it()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.CreateAsync(Input(cardScheme: "Mastercard"));

        result.Status.Should().Be(BinRangeMutationStatus.PrefixInUse);
        result.Error.Should().Contain("400001");

        var stored = await NewContext().BinRanges.SingleAsync();
        stored.CardSchemeId.Should().Be(VisaId);
    }

    [Fact]
    public async Task Adding_a_prefix_whose_only_record_was_deleted_revives_that_record()
    {
        // The unique index spans deleted rows, so there is no second record to insert.
        var deleted = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        deleted.IsDeleted = true;
        deleted.DeletedBy = "someone";
        Db.SaveChanges();

        // Reviving with a scheme that contradicts the prefix would trip the new detector
        // check; this test is about revival, not the check, so it acknowledges up front.
        var result = await _service.CreateAsync(
            Input(cardScheme: "Mastercard", acknowledgeSchemeMismatch: true));

        result.Status.Should().Be(BinRangeMutationStatus.Restored);
        result.Range!.BinRangeId.Should().Be(deleted.BinRangeId, "the row keeps its identity");
        result.Range.CardScheme.Should().Be("Mastercard", "the new values are applied");
        result.Range.Status.Should().Be(BinRangeStatus.Active);

        var stored = await NewContext().BinRanges.SingleAsync();
        stored.IsDeleted.Should().BeFalse();
        stored.DeletedBy.Should().BeNull();
    }

    [Theory]
    [InlineData("40000", "6 to 8 digits")]
    [InlineData("400001234", "6 to 8 digits")]
    [InlineData("4000a1", "6 to 8 digits")]
    [InlineData("", "required")]
    public async Task A_prefix_that_is_not_6_to_8_digits_is_rejected(string prefix, string expected)
    {
        var result = await _service.CreateAsync(Input(prefix: prefix));

        result.Status.Should().Be(BinRangeMutationStatus.Invalid);
        result.Error.Should().Contain(expected);
    }

    [Fact]
    public async Task A_valid_to_that_is_not_after_valid_from_is_rejected()
    {
        var result = await _service.CreateAsync(Input(validFrom: Started, validTo: Started));

        result.Status.Should().Be(BinRangeMutationStatus.Invalid);
        result.Error.Should().Contain("after ValidFrom");
    }

    [Fact]
    public async Task Naming_reference_data_that_does_not_exist_is_rejected_rather_than_creating_it()
    {
        var result = await _service.CreateAsync(
            Input(cardScheme: "Discover", countryCode: "ZZ"));

        result.Status.Should().Be(BinRangeMutationStatus.Invalid);
        result.Error.Should().Contain("Discover").And.Contain("ZZ",
            "fixing one name only to be told about the next makes for a poor form");

        (await NewContext().BinRanges.CountAsync()).Should().Be(0);
        (await NewContext().CardSchemes.AnyAsync(c => c.Name == "Discover")).Should().BeFalse();
    }

    // ---- Prefix vs declared scheme ---------------------------------------------

    [Fact]
    public async Task Adding_a_prefix_whose_network_contradicts_the_declared_scheme_is_refused()
    {
        // 520001 is Mastercard; the form says Visa.
        var result = await _service.CreateAsync(Input(prefix: "520001", cardScheme: "Visa"));

        result.Status.Should().Be(BinRangeMutationStatus.SchemeMismatch);
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Mastercard").And.Contain("Visa");

        (await NewContext().BinRanges.CountAsync())
            .Should().Be(0, "the row is refused, not saved silently");
    }

    [Fact]
    public async Task Acknowledging_the_scheme_mismatch_saves_the_row_as_declared()
    {
        // Same input, but this time the caller has ticked the override.
        var result = await _service.CreateAsync(Input(
            prefix: "520001", cardScheme: "Visa", acknowledgeSchemeMismatch: true));

        result.Status.Should().Be(BinRangeMutationStatus.Created);
        result.Range!.CardScheme.Should().Be("Visa", "the caller is on record as choosing to override");
    }

    [Fact]
    public async Task A_prefix_the_detector_does_not_recognise_is_refused_without_the_override()
    {
        // 990000 falls outside every known IIN range. The declared scheme is fine on its
        // own - the point of the check is that the detector cannot vouch for it.
        var result = await _service.CreateAsync(Input(prefix: "990000", cardScheme: "Visa"));

        result.Status.Should().Be(BinRangeMutationStatus.SchemeMismatch);
        result.Error.Should().Contain("does not match any known").And.Contain("Visa");
    }

    [Fact]
    public async Task Editing_the_scheme_to_one_that_contradicts_the_prefix_is_refused()
    {
        // The row exists as Visa on a Visa prefix; the edit tries to relabel it Mastercard
        // without acknowledging the mismatch.
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.UpdateAsync(
            range.BinRangeId, Input(cardScheme: "Mastercard"));

        result.Status.Should().Be(BinRangeMutationStatus.SchemeMismatch);
        (await NewContext().BinRanges.SingleAsync()).CardSchemeId
            .Should().Be(VisaId, "the refusal must not partially apply");
    }

    // ---- Update ----------------------------------------------------------------

    [Fact]
    public async Task Editing_a_range_overwrites_it_in_place()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        // Prefix stays 400001 (Visa range) but the scheme moves to Mastercard - this test
        // is about the overwrite, not the scheme check, so it acknowledges the mismatch.
        var result = await _service.UpdateAsync(range.BinRangeId, Input(
            cardScheme: "Mastercard", fundingType: "Debit", countryCode: "BG",
            validTo: new DateTime(2030, 1, 1), acknowledgeSchemeMismatch: true));

        result.Status.Should().Be(BinRangeMutationStatus.Updated);
        result.Range!.BinRangeId.Should().Be(range.BinRangeId);
        result.Range.CardScheme.Should().Be("Mastercard");
        result.Range.FundingType.Should().Be("Debit");
        result.Range.CountryCode.Should().Be("BG");
        result.Range.UpdatedBy.Should().Be("admin");

        (await NewContext().BinRanges.CountAsync()).Should().Be(1, "editing must not insert");
    }

    [Fact]
    public async Task Editing_can_change_the_prefix()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.UpdateAsync(range.BinRangeId, Input(prefix: "40000199"));

        result.Status.Should().Be(BinRangeMutationStatus.Updated);
        result.Range!.Prefix.Should().Be("40000199");
        result.Range.PrefixLength.Should().Be(8, "length is derived, not supplied");
    }

    [Fact]
    public async Task Editing_onto_a_prefix_another_range_owns_is_refused()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        SeedBinRange("520000", MastercardId, ConsumerId, DebitId, BgCountryId, Started);

        var result = await _service.UpdateAsync(range.BinRangeId, Input(prefix: "520000"));

        result.Status.Should().Be(BinRangeMutationStatus.PrefixInUse);
    }

    [Fact]
    public async Task Editing_a_range_to_the_prefix_it_already_has_is_allowed()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.UpdateAsync(
            range.BinRangeId, Input(prefix: "400001", productType: "Commercial"));

        result.Status.Should().Be(BinRangeMutationStatus.Updated);
        result.Range!.ProductType.Should().Be("Commercial");
    }

    [Fact]
    public async Task Editing_a_range_that_does_not_exist_is_a_not_found()
    {
        var result = await _service.UpdateAsync(999, Input());

        result.Status.Should().Be(BinRangeMutationStatus.NotFound);
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task A_deleted_range_must_be_restored_before_it_can_be_edited()
    {
        // Otherwise a correction would quietly bring it back as a side effect.
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        range.IsDeleted = true;
        Db.SaveChanges();

        var result = await _service.UpdateAsync(range.BinRangeId, Input(cardScheme: "Mastercard"));

        result.Status.Should().Be(BinRangeMutationStatus.NotFound);
        result.Error.Should().Contain("Restore it");

        (await NewContext().BinRanges.SingleAsync()).IsDeleted.Should().BeTrue();
    }

    // ---- Delete and restore ----------------------------------------------------

    [Fact]
    public async Task Deleting_a_range_is_soft_and_records_who_did_it()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.DeleteAsync(range.BinRangeId);

        result.Status.Should().Be(BinRangeMutationStatus.Deleted);
        result.Range!.Status.Should().Be(BinRangeStatus.Deleted);

        var stored = await NewContext().BinRanges.SingleAsync();
        stored.IsDeleted.Should().BeTrue();
        stored.DeletedBy.Should().Be("admin");
        stored.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task A_deleted_range_no_longer_classifies()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        await _service.DeleteAsync(range.BinRangeId);

        var context = NewContext();
        var classification = await new BinClassificationService(
                context, new CommissionResolver(context), new CardSchemeDetector())
            .ClassifyAsync("4000011234567");

        classification.Matched.Should().BeFalse();
    }

    [Fact]
    public async Task Deleting_a_range_twice_is_refused_rather_than_silently_accepted()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        await _service.DeleteAsync(range.BinRangeId);

        var result = await _service.DeleteAsync(range.BinRangeId);

        result.Status.Should().Be(BinRangeMutationStatus.AlreadyInThatState);
    }

    [Fact]
    public async Task Restoring_brings_a_range_back_with_the_values_it_had()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            Started, validTo: Today.AddYears(1));
        await _service.DeleteAsync(range.BinRangeId);

        var result = await _service.RestoreAsync(range.BinRangeId);

        result.Status.Should().Be(BinRangeMutationStatus.Restored);
        result.Range!.Status.Should().Be(BinRangeStatus.Active);
        result.Range.CardScheme.Should().Be("Visa");
        result.Range.ValidTo.Should().Be(Today.AddYears(1));

        var stored = await NewContext().BinRanges.SingleAsync();
        stored.IsDeleted.Should().BeFalse();
        stored.DeletedAt.Should().BeNull();
        stored.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task Restoring_a_range_that_was_never_deleted_is_refused()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await _service.RestoreAsync(range.BinRangeId);

        result.Status.Should().Be(BinRangeMutationStatus.AlreadyInThatState);
    }

    [Fact]
    public async Task Deleting_or_restoring_a_range_that_does_not_exist_is_a_not_found()
    {
        (await _service.DeleteAsync(999)).Status.Should().Be(BinRangeMutationStatus.NotFound);
        (await _service.RestoreAsync(999)).Status.Should().Be(BinRangeMutationStatus.NotFound);
    }

    // ---- Get -------------------------------------------------------------------

    [Fact]
    public async Task Get_returns_deleted_ranges_too_so_one_can_be_inspected_before_restoring()
    {
        var range = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        await _service.DeleteAsync(range.BinRangeId);

        var found = await _service.GetAsync(range.BinRangeId);

        found.Should().NotBeNull();
        found!.Status.Should().Be(BinRangeStatus.Deleted);

        (await _service.GetAsync(999)).Should().BeNull();
    }

    private sealed class TestUser : ICurrentUser
    {
        public TestUser(string userId, string name)
        {
            UserId = userId;
            Name = name;
        }

        public string? UserId { get; }

        public string Name { get; }
    }
}
