using BinTool.Application.Models.Commission;
using BinTool.Application.Services;
using BinTool.Infrastructure.Repositories;
using FluentAssertions;

namespace BinTool.Tests;

// Commission rule writes against a real database: the ambiguity guard, the range checks and the
// score suggestion, all the way down to SQL. The same rules are covered far more cheaply in
// CommissionRuleAdminServiceUnitTests against a fake repository. These exist to prove the two agree
// - that what the service decides in memory survives being written and read back through Entity
// Framework.
public class CommissionRuleAdminServiceTests : SqliteTestBase
{
    private const string AdminUserId = "admin-user-id";
    private readonly CommissionRuleAdminService _service;

    public CommissionRuleAdminServiceTests()
    {
        SeedUser(AdminUserId, "admin");
        var currentUser = new TestUser(AdminUserId, "admin");
        _service = new CommissionRuleAdminService(
            new CommissionRuleRepository(Db),
            new ReferenceDataRepository(Db),
            currentUser,
            new AuditLog(new AuditRepository(Db), currentUser),
            new RecordingLogger<CommissionRuleAdminService>());
    }

    private static CommissionRuleInput Input(
        string name = "Test rule",
        int? schemeId = null,
        int? productId = null,
        int? fundingId = null,
        int? regionId = null,
        int priority = 0,
        int? priorityScore = null,
        DateTime? validFrom = null,
        DateTime? validTo = null) => new()
        {
            RuleName = name,
            CardSchemeId = schemeId,
            ProductTypeId = productId,
            FundingTypeId = fundingId,
            RegionId = regionId,
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
    public async Task Co_matchable_rules_with_same_priority_and_score_are_refused()
    {
        var first = await _service.CreateAsync(Input(
            name: "Visa/Consumer", schemeId: VisaId, productId: ConsumerId, priority: 5));
        first.Status.Should().Be(CommissionRuleMutationStatus.Created);

        var second = await _service.CreateAsync(Input(
            name: "Visa/Credit", schemeId: VisaId, fundingId: CreditId, priority: 5));

        second.Status.Should().Be(CommissionRuleMutationStatus.Overlap);
        second.Error.Should().Contain("same priority");
    }

    [Fact]
    public async Task Co_matchable_rules_with_different_priority_are_accepted()
    {
        await _service.CreateAsync(Input(
            name: "Visa/Consumer", schemeId: VisaId, productId: ConsumerId, priority: 5));

        var second = await _service.CreateAsync(Input(
            name: "Visa/Credit", schemeId: VisaId, fundingId: CreditId, priority: 10));

        second.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    [Fact]
    public async Task Co_matchable_rules_with_non_overlapping_windows_are_accepted()
    {
        await _service.CreateAsync(Input(
            name: "Visa/Consumer", schemeId: VisaId, productId: ConsumerId, priority: 5,
            validFrom: new DateTime(2025, 1, 1), validTo: new DateTime(2025, 6, 30)));

        var second = await _service.CreateAsync(Input(
            name: "Visa/Credit", schemeId: VisaId, fundingId: CreditId, priority: 5,
            validFrom: new DateTime(2025, 7, 1)));

        second.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    [Fact]
    public async Task Non_co_matchable_rules_are_accepted_even_at_equal_numbers()
    {
        await _service.CreateAsync(Input(
            name: "Visa only", schemeId: VisaId, priority: 5));

        var second = await _service.CreateAsync(Input(
            name: "Mastercard only", schemeId: MastercardId, priority: 5));

        second.Status.Should().Be(CommissionRuleMutationStatus.Created);
    }

    // ---- Priority range ---------------------------------------------------------

    [Fact]
    public async Task Priority_above_100_is_rejected()
    {
        var result = await _service.CreateAsync(Input(priority: 101));

        result.Status.Should().Be(CommissionRuleMutationStatus.Invalid);
    }

    [Fact]
    public async Task Priority_score_above_4_is_rejected()
    {
        var result = await _service.CreateAsync(Input(priorityScore: 5));

        result.Status.Should().Be(CommissionRuleMutationStatus.Invalid);
    }

    // ---- PriorityScore suggestion and override ----------------------------------

    [Fact]
    public async Task Null_priority_score_stores_the_suggested_count()
    {
        var result = await _service.CreateAsync(Input(
            name: "Two fields", schemeId: VisaId, productId: ConsumerId));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
        result.Rule!.PriorityScore.Should().Be(2);
    }

    [Fact]
    public async Task Explicit_priority_score_overrides_the_suggestion()
    {
        var result = await _service.CreateAsync(Input(
            name: "Override", schemeId: VisaId, productId: ConsumerId, priorityScore: 4));

        result.Status.Should().Be(CommissionRuleMutationStatus.Created);
        result.Rule!.PriorityScore.Should().Be(4);
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
