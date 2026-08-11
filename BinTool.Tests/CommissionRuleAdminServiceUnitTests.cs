using BinTool.Application.Models.Commission;
using BinTool.Application.Services;
using BinTool.Tests.Fakes;
using FluentAssertions;

namespace BinTool.Tests;

/// <summary>
/// The service's own decisions, against a fake repository. No database, no schema, no seeded
/// reference data - which is the point: what is being tested here is the rule the service
/// applies, and a rule that needs SQLite to be observed was never really separable from it.
/// </summary>
public class CommissionRuleAdminServiceUnitTests
{
    private const int Visa = 1, Mastercard = 2;
    private const int Consumer = 1;
    private const int Credit = 1;
    private const int Eur = 1;

    private readonly FakeCommissionRuleRepository _rules = new();
    private readonly RecordingAuditLog _audit = new();
    private readonly CommissionRuleAdminService _service;

    public CommissionRuleAdminServiceUnitTests()
    {
        _service = new CommissionRuleAdminService(
            _rules, new FakeReferenceDataRepository(), new StubCurrentUser(), _audit);
    }

    private static CommissionRuleInput Input(
        string name = "Test rule",
        int? scheme = null,
        int? product = null,
        int? funding = null,
        int? region = null,
        int priority = 0,
        int? priorityScore = null,
        int currencyId = Eur,
        DateTime? validFrom = null,
        DateTime? validTo = null) => new()
    {
        RuleName = name,
        CardSchemeId = scheme,
        ProductTypeId = product,
        FundingTypeId = funding,
        RegionId = region,
        CurrencyId = currencyId,
        Priority = priority,
        PriorityScore = priorityScore,
        PercentageRate = 1.0m,
        FixedAmount = 0.10m,
        MinimumFee = 0.05m,
        ValidFrom = validFrom ?? new DateTime(2025, 1, 1),
        ValidTo = validTo,
        IsActive = true
    };

    // ---- Ambiguity guard --------------------------------------------------------

    [Fact]
    public async Task Co_matchable_rules_at_the_same_priority_and_score_are_refused()
    {
        _rules.Seed("Visa/Consumer", priority: 5, priorityScore: 2,
            scheme: Visa, product: Consumer);

        var result = await _service.CreateAsync(Input(
            name: "Visa/Credit", scheme: Visa, funding: Credit, priority: 5));

        result.Status.Should().Be(CommissionRuleMutationStatus.Overlap);
        result.Error.Should().Contain("same priority");
        result.ConflictingRuleName.Should().Be("Visa/Consumer");
    }

    [Fact]
    public async Task A_different_priority_separates_two_co_matchable_rules()
    {
        _rules.Seed("Visa/Consumer", priority: 5, priorityScore: 2,
            scheme: Visa, product: Consumer);

        var result = await _service.CreateAsync(Input(
            name: "Visa/Credit", scheme: Visa, funding: Credit, priority: 10));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    [Fact]
    public async Task A_different_score_separates_two_co_matchable_rules()
    {
        _rules.Seed("Visa/Consumer", priority: 5, priorityScore: 2,
            scheme: Visa, product: Consumer);

        var result = await _service.CreateAsync(Input(
            name: "Visa/Credit", scheme: Visa, funding: Credit, priority: 5, priorityScore: 7));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    [Fact]
    public async Task Rules_that_cannot_match_the_same_card_may_share_both_numbers()
    {
        _rules.Seed("Visa only", priority: 5, priorityScore: 1, scheme: Visa);

        var result = await _service.CreateAsync(Input(
            name: "Mastercard only", scheme: Mastercard, priority: 5));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    [Fact]
    public async Task Windows_that_never_coincide_may_share_both_numbers()
    {
        _rules.Seed("First half", priority: 5, priorityScore: 2,
            scheme: Visa, product: Consumer,
            validFrom: new DateTime(2025, 1, 1), validTo: new DateTime(2025, 6, 30));

        var result = await _service.CreateAsync(Input(
            name: "Second half", scheme: Visa, funding: Credit, priority: 5,
            validFrom: new DateTime(2025, 7, 1)));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    [Fact]
    public async Task A_deleted_rule_does_not_block_a_new_one()
    {
        _rules.Seed("Withdrawn", priority: 5, priorityScore: 2,
            scheme: Visa, product: Consumer, isDeleted: true);

        var result = await _service.CreateAsync(Input(
            name: "Replacement", scheme: Visa, product: Consumer, priority: 5));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    [Fact]
    public async Task A_rule_being_edited_does_not_conflict_with_itself()
    {
        var existing = _rules.Seed("Visa/Consumer", priority: 5, priorityScore: 2,
            scheme: Visa, product: Consumer);

        var result = await _service.UpdateAsync(existing.CommissionRuleId, Input(
            name: "Visa/Consumer renamed", scheme: Visa, product: Consumer, priority: 5));

        result.Status.Should().Be(CommissionRuleMutationStatus.Updated);
    }

    // ---- Identical keys ---------------------------------------------------------

    [Fact]
    public async Task An_identical_key_overlapping_in_time_is_refused_as_a_duplicate()
    {
        // Different priority, so the ambiguity guard would let this through - but two rules
        // saying different things about exactly the same cards is a duplicate tariff.
        _rules.Seed("Original", priority: 1, priorityScore: 2, scheme: Visa, product: Consumer);

        var result = await _service.CreateAsync(Input(
            name: "Duplicate", scheme: Visa, product: Consumer, priority: 9));

        result.Status.Should().Be(CommissionRuleMutationStatus.Overlap);
        result.Error.Should().Contain("overlaps");
    }

    // ---- Score suggestion and override -----------------------------------------

    [Fact]
    public async Task An_unset_score_stores_the_number_of_pinned_fields()
    {
        var result = await _service.CreateAsync(Input(scheme: Visa, product: Consumer));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
        result.Rule!.PriorityScore.Should().Be(2);
    }

    [Fact]
    public async Task An_explicit_score_overrides_the_suggestion()
    {
        var result = await _service.CreateAsync(Input(
            scheme: Visa, product: Consumer, priorityScore: 7));

        result.Rule!.PriorityScore.Should().Be(7);
        result.Rule.Specificity.Should().Be(2, "the suggestion is still reported alongside it");
    }

    // ---- Validation -------------------------------------------------------------

    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task A_priority_outside_the_allowed_range_is_rejected(int priority)
    {
        var result = await _service.CreateAsync(Input(priority: priority));

        result.Status.Should().Be(CommissionRuleMutationStatus.Invalid);
        result.Error.Should().Contain("Priority");
    }

    [Fact]
    public async Task A_score_above_the_allowed_range_is_rejected()
    {
        var result = await _service.CreateAsync(Input(priorityScore: 101));

        result.Status.Should().Be(CommissionRuleMutationStatus.Invalid);
    }

    [Fact]
    public async Task An_end_date_before_the_start_date_is_rejected()
    {
        var result = await _service.CreateAsync(Input(
            validFrom: new DateTime(2025, 6, 1), validTo: new DateTime(2025, 1, 1)));

        result.Status.Should().Be(CommissionRuleMutationStatus.Invalid);
        result.Error.Should().Contain("ValidTo");
    }

    [Fact]
    public async Task A_key_id_that_does_not_resolve_is_rejected()
    {
        var result = await _service.CreateAsync(Input(scheme: 999));

        result.Status.Should().Be(CommissionRuleMutationStatus.Invalid);
        result.Error.Should().Contain("Card scheme 999");
    }

    [Fact]
    public async Task Every_unresolved_id_is_reported_at_once()
    {
        var result = await _service.CreateAsync(Input(scheme: 999, product: 998, region: 997));

        result.Error.Should().Contain("999").And.Contain("998").And.Contain("997");
    }

    // ---- Auditing and the unit of work -----------------------------------------

    [Fact]
    public async Task A_create_is_audited_and_committed_in_one_transaction()
    {
        await _service.CreateAsync(Input(scheme: Visa));

        _rules.TransactionsStarted.Should().Be(1);
        _rules.TransactionsCommitted.Should().Be(1);

        _audit.Entries.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.Created);
    }

    [Fact]
    public async Task A_refused_create_writes_and_audits_nothing()
    {
        _rules.Seed("Visa/Consumer", priority: 5, priorityScore: 2,
            scheme: Visa, product: Consumer);

        await _service.CreateAsync(Input(
            name: "Clash", scheme: Visa, funding: Credit, priority: 5));

        _rules.SaveCount.Should().Be(0);
        _rules.TransactionsStarted.Should().Be(0);
        _audit.Entries.Should().BeEmpty();
    }

    // ---- Delete, restore and the default ----------------------------------------

    [Fact]
    public async Task The_default_rule_cannot_be_deleted()
    {
        var rule = _rules.Seed("House default", priority: 0, priorityScore: 0);
        await _service.SetDefaultAsync(rule.CommissionRuleId);

        var result = await _service.DeleteAsync(rule.CommissionRuleId);

        result.Status.Should().Be(CommissionRuleMutationStatus.InUse);
    }

    [Fact]
    public async Task A_deleted_rule_cannot_be_edited_until_it_is_restored()
    {
        var rule = _rules.Seed("Withdrawn", isDeleted: true);

        var result = await _service.UpdateAsync(rule.CommissionRuleId, Input());

        result.Status.Should().Be(CommissionRuleMutationStatus.NotFound);
        result.Error.Should().Contain("Restore it");
    }

    [Fact]
    public async Task Deleting_twice_reports_that_nothing_changed()
    {
        var rule = _rules.Seed("Withdrawn", isDeleted: true);

        var result = await _service.DeleteAsync(rule.CommissionRuleId);

        result.Status.Should().Be(CommissionRuleMutationStatus.AlreadyInThatState);
    }

    [Fact]
    public async Task An_inactive_rule_cannot_become_the_default()
    {
        var rule = _rules.Seed("Dormant", isActive: false);

        var result = await _service.SetDefaultAsync(rule.CommissionRuleId);

        result.Status.Should().Be(CommissionRuleMutationStatus.Invalid);
    }

    [Fact]
    public async Task Clearing_a_default_that_was_never_set_still_succeeds()
    {
        var result = await _service.ClearDefaultAsync();

        result.Status.Should().Be(CommissionRuleMutationStatus.Updated);
    }

    [Fact]
    public async Task A_missing_rule_reports_not_found()
    {
        var result = await _service.UpdateAsync(404, Input());

        result.Status.Should().Be(CommissionRuleMutationStatus.NotFound);
    }
}
