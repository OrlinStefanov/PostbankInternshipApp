using BinTool.Domain.Entities;
using BinTool.Infrastructure.Services;
using FluentAssertions;

namespace BinTool.Tests;

/// <summary>
/// Rule resolution and fee calculation (Epic 5, stories 5.2 and 5.4). Rules are seeded
/// straight into the shared SQLite database so a test controls the criteria, validity and
/// rates precisely, then <see cref="CommissionResolver"/> is run against them.
/// <para>
/// A null criteria field is a wildcard: the most specific matching rule (fewest wildcards)
/// wins, only rules valid on the transaction date are considered, and when nothing matches
/// the configured default is used and the result is flagged as a fallback.
/// </para>
/// </summary>
public class CommissionResolverTests : SqliteTestBase
{
    private const int DomesticRegionId = 1;

    // A date every "valid" seeded rule below covers.
    private static readonly DateTime OnDate = new(2025, 6, 1);

    private CommissionResolver Resolver() => new(Db);

    /// <summary>
    /// Seeds a rule with one criteria row. A null scheme/product/funding/region is a
    /// wildcard. Valid from a year before <see cref="OnDate"/> and open-ended unless a
    /// <paramref name="validTo"/> is given.
    /// </summary>
    private const int EurCurrencyId = 1;

    private CommissionRule SeedRule(
        string name,
        decimal percentage = 0m,
        decimal fixedAmount = 0m,
        decimal minimumFee = 0m,
        int? scheme = null,
        int? product = null,
        int? funding = null,
        int? region = null,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        bool isActive = true,
        int priority = 0,
        int? priorityScore = null,
        int currencyId = EurCurrencyId)
    {
        var computedScore = (scheme is null ? 0 : 1)
            + (product is null ? 0 : 1)
            + (funding is null ? 0 : 1)
            + (region is null ? 0 : 1);

        var rule = new CommissionRule
        {
            RuleName = name,
            CurrencyId = currencyId,
            PercentageRate = percentage,
            FixedAmount = fixedAmount,
            MinimumFee = minimumFee,
            Priority = priority,
            ValidFrom = validFrom ?? OnDate.AddYears(-1),
            ValidTo = validTo,
            IsActive = isActive,
            RuleCriteria = new List<RuleCriteria>
            {
                new()
                {
                    CardSchemeId = scheme,
                    ProductTypeId = product,
                    FundingTypeId = funding,
                    RegionId = region,
                    PriorityScore = priorityScore ?? computedScore
                }
            }
        };

        Db.CommissionRules.Add(rule);
        Db.SaveChanges();
        return rule;
    }

    private void SetDefault(CommissionRule rule)
    {
        Db.DefaultRules.Add(new DefaultRule
        {
            CommissionRuleId = rule.CommissionRuleId,
            IsSystemDefault = true
        });
        Db.SaveChanges();
    }

    private Task<Application.Models.Commission.CommissionCalculation?> Resolve(
        decimal amount = 100m, int? inputCurrencyId = null) =>
        Resolver().ResolveAsync(
            VisaId, ConsumerId, CreditId, DomesticRegionId, amount, OnDate, inputCurrencyId);

    /// <summary>Adds a currency with a clean euro rate and returns its id.</summary>
    private int SeedCurrency(string code, decimal rateToEur)
    {
        var currency = new Currency { Code = code, Name = code, RateToEur = rateToEur };
        Db.Currencies.Add(currency);
        Db.SaveChanges();
        return currency.CurrencyId;
    }

    // ---- Story 5.2: resolution by specificity ---------------------------------

    [Fact]
    public async Task An_exactly_matching_rule_is_selected()
    {
        var rule = SeedRule("Visa/Consumer/Credit/Domestic",
            scheme: VisaId, product: ConsumerId, funding: CreditId, region: DomesticRegionId);

        var result = await Resolve();

        result.Should().NotBeNull();
        result!.AppliedRuleId.Should().Be(rule.CommissionRuleId);
        result.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task A_wildcard_rule_matches_when_no_specific_rule_exists()
    {
        var rule = SeedRule("Any card"); // all four fields null

        var result = await Resolve();

        result.Should().NotBeNull();
        result!.AppliedRuleId.Should().Be(rule.CommissionRuleId);
        result.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task An_exact_rule_beats_a_wildcard_rule()
    {
        SeedRule("Any card");
        var exact = SeedRule("Visa exact",
            scheme: VisaId, product: ConsumerId, funding: CreditId, region: DomesticRegionId);

        var result = await Resolve();

        result!.AppliedRuleId.Should().Be(exact.CommissionRuleId);
    }

    [Fact]
    public async Task An_expired_rule_is_ignored_in_favour_of_a_valid_one()
    {
        // The more specific rule has expired, so the still-valid wildcard must win even
        // though it is less specific.
        SeedRule("Visa exact (expired)",
            scheme: VisaId, product: ConsumerId, funding: CreditId, region: DomesticRegionId,
            validFrom: OnDate.AddYears(-2), validTo: OnDate.AddMonths(-1));
        var open = SeedRule("Any card (valid)");

        var result = await Resolve();

        result!.AppliedRuleId.Should().Be(open.CommissionRuleId);
    }

    [Fact]
    public async Task An_open_ended_rule_valid_from_the_past_matches()
    {
        var rule = SeedRule("Open ended", validFrom: OnDate.AddYears(-5), validTo: null);

        var result = await Resolve();

        result!.AppliedRuleId.Should().Be(rule.CommissionRuleId);
    }

    [Fact]
    public async Task No_matching_rule_and_no_default_returns_null()
    {
        // A rule that cannot match the Visa card being priced, and no default configured.
        SeedRule("Mastercard only", scheme: MastercardId);

        var result = await Resolve();

        result.Should().BeNull();
    }

    [Fact]
    public async Task No_matching_rule_falls_back_to_the_default_and_is_flagged()
    {
        var fallback = SeedRule("House default", scheme: MastercardId,
            percentage: 1.0m, fixedAmount: 0.10m);
        SetDefault(fallback);

        var result = await Resolve();

        result.Should().NotBeNull();
        result!.IsFallback.Should().BeTrue();
        result.AppliedRuleId.Should().Be(fallback.CommissionRuleId);
        result.Reason.Should().ContainEquivalentOf("default").And.Contain("House default");
    }

    [Fact]
    public async Task Equally_specific_rules_resolve_deterministically()
    {
        // Two rules that match equally well, with the same priority and validity, so the
        // only thing left to separate them is the final id tiebreak. Whatever the storage
        // order, the outcome is fixed - which is what makes resolution independent of the
        // order rules were entered.
        var first = SeedRule("Any card A");
        var second = SeedRule("Any card B");

        var result = await Resolve();

        result!.AppliedRuleId.Should().Be(Math.Min(first.CommissionRuleId, second.CommissionRuleId));
    }

    [Fact]
    public async Task Higher_priority_beats_a_more_specific_rule()
    {
        SeedRule("Visa exact (low priority)",
            scheme: VisaId, product: ConsumerId, funding: CreditId, region: DomesticRegionId,
            priority: 1);
        var broad = SeedRule("Any card (high priority)", priority: 10);

        var result = await Resolve();

        result!.AppliedRuleId.Should().Be(broad.CommissionRuleId);
    }

    [Fact]
    public async Task At_equal_priority_the_higher_priority_score_wins()
    {
        var low = SeedRule("Low score", priority: 5, priorityScore: 1);
        var high = SeedRule("High score", priority: 5, priorityScore: 3);

        var result = await Resolve();

        result!.AppliedRuleId.Should().Be(high.CommissionRuleId);
    }

    [Fact]
    public async Task An_overridden_priority_score_beats_the_computed_one()
    {
        SeedRule("Specific but low override",
            scheme: VisaId, product: ConsumerId, funding: CreditId, region: DomesticRegionId,
            priority: 5, priorityScore: 1);
        var overridden = SeedRule("Wildcard with high override",
            priority: 5, priorityScore: 10);

        var result = await Resolve();

        result!.AppliedRuleId.Should().Be(overridden.CommissionRuleId);
    }

    // ---- Story 5.4: fee calculation -------------------------------------------

    [Fact]
    public async Task Fee_is_percentage_plus_fixed_amount()
    {
        // 100.00 @ 0.85% = 0.85, + 0.12 fixed = 0.97, above the 0.20 minimum.
        SeedRule("Standard", percentage: 0.85m, fixedAmount: 0.12m, minimumFee: 0.20m);

        var result = await Resolve(amount: 100m);

        result!.Fee.Should().Be(0.97m);
        result.MinimumApplied.Should().BeFalse();
    }

    [Fact]
    public async Task The_minimum_fee_floors_a_small_calculated_fee()
    {
        // 1.00 @ 0.85% = 0.0085, + 0.12 = 0.1285, below the 0.20 minimum, so the fee is
        // raised to the minimum.
        SeedRule("Standard", percentage: 0.85m, fixedAmount: 0.12m, minimumFee: 0.20m);

        var result = await Resolve(amount: 1m);

        result!.Fee.Should().Be(0.20m);
        result.MinimumApplied.Should().BeTrue();
    }

    [Fact]
    public async Task The_fee_uses_bankers_rounding_at_a_midpoint()
    {
        // A raw fee of exactly 0.125 rounds to the even hundredth (0.12), not up to 0.13 -
        // proving MidpointRounding.ToEven is applied consistently.
        SeedRule("Half-cent", percentage: 0m, fixedAmount: 0.125m, minimumFee: 0m);

        var result = await Resolve(amount: 100m);

        result!.Fee.Should().Be(0.12m);
    }

    // ---- Currency: euro equivalents and cross-currency input -------------------

    [Fact]
    public async Task A_euro_rule_reports_euro_at_a_unit_rate_with_matching_equivalents()
    {
        SeedRule("Standard", percentage: 0.85m, fixedAmount: 0.12m, minimumFee: 0.20m);

        var result = await Resolve(amount: 100m);

        result!.CurrencyCode.Should().Be("EUR");
        result.InputCurrencyCode.Should().Be("EUR");
        result.EurRate.Should().Be(1m);
        result.Fee.Should().Be(0.97m);
        result.FeeEur.Should().Be(result.Fee, "a euro rule needs no conversion");
    }

    [Fact]
    public async Task A_rule_priced_in_another_currency_reports_the_euro_equivalent()
    {
        // A currency worth half a euro per unit, priced in its own currency (no input
        // conversion): a 2.00 fixed fee is 1.00 EUR.
        var half = SeedCurrency("HAF", 0.5m);
        SeedRule("Fixed only", fixedAmount: 2.00m, currencyId: half);

        var result = await Resolve(amount: 100m, inputCurrencyId: half);

        result!.CurrencyCode.Should().Be("HAF");
        result.EurRate.Should().Be(0.5m);
        result.Fee.Should().Be(2.00m);
        result.FeeEur.Should().Be(1.00m, "2.00 HAF at 0.5 EUR/unit is 1.00 EUR");
    }

    [Fact]
    public async Task An_amount_in_another_currency_is_converted_into_the_rules_currency()
    {
        // Rule is in euro; the amount is entered in a currency worth 0.5 EUR per unit, so
        // 100 units convert to 50 EUR, and 1% of that is 0.50 EUR.
        var half = SeedCurrency("HAF", 0.5m);
        SeedRule("One percent", percentage: 1.0m);

        var result = await Resolve(amount: 100m, inputCurrencyId: half);

        result!.InputCurrencyCode.Should().Be("HAF");
        result.InputAmount.Should().Be(100m);
        result.CurrencyCode.Should().Be("EUR");
        result.Amount.Should().Be(50m, "100 HAF at 0.5 EUR/unit is 50 EUR");
        result.Fee.Should().Be(0.50m);
    }
}
