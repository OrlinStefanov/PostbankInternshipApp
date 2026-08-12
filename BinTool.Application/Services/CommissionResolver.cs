using BinTool.Application.Abstractions;
using BinTool.Application.Models.Commission;
using BinTool.Domain.Common;
using BinTool.Domain.Entities;

namespace BinTool.Application.Services;

// Selection is deterministic: among the rules matching the card and valid on the date, the highest
// Priority wins; ties break by PriorityScore, then ValidFrom (newer wins), then row id. Nothing
// matching uses the configured default and flags the result as a fallback.
public class CommissionResolver : ICommissionResolver
{
    private readonly ICommissionRuleRepository _rules;
    private readonly ICurrencyRepository _currencies;

    public CommissionResolver(ICommissionRuleRepository rules, ICurrencyRepository currencies)
    {
        _rules = rules;
        _currencies = currencies;
    }

    public async Task<CommissionCalculation?> ResolveAsync(
        int cardSchemeId,
        int productTypeId,
        int fundingTypeId,
        int regionId,
        decimal amount,
        DateTime onDate,
        int? inputCurrencyId = null,
        CancellationToken cancellationToken = default)
    {
        var day = onDate.Date;

        // The currency the amount was quoted in, so the fee can be worked out in the rule's
        // currency even when the two differ. Falls back to the euro base currency.
        var inputCurrency = await ResolveInputCurrencyAsync(inputCurrencyId, cancellationToken);

        var matches = await _rules.FindMatchingAsync(
            cardSchemeId, productTypeId, fundingTypeId, regionId, day, cancellationToken);

        // The matching set is small, so the tiebreak is settled here where it reads as the
        // ranking it is rather than as an ORDER BY four clauses long.
        var winner = matches
            .OrderByDescending(r => r.Priority)          // the admin's ranking
            .ThenByDescending(r => r.PriorityScore())    // the stored score
            .ThenByDescending(r => r.ValidFrom)          // newer tariff wins
            .ThenBy(r => r.CommissionRuleId)             // deterministic floor
            .FirstOrDefault();

        if (winner is not null)
        {
            return Calculate(winner, amount, inputCurrency, isFallback: false);
        }

        // No rule matched. Fall back to the configured default, if one is set and still live.
        var fallback = await _rules.GetLiveDefaultRuleAsync(cancellationToken);

        return fallback is null
            ? null
            : Calculate(fallback, amount, inputCurrency, isFallback: true);
    }

    // Loads the currency the amount was quoted in. A null id, or an id that no longer resolves to a
    // live currency, is treated as the euro base currency so a price is still produced rather than
    // silently dropped.
    private async Task<Currency> ResolveInputCurrencyAsync(
        int? inputCurrencyId, CancellationToken cancellationToken)
    {
        if (inputCurrencyId is { } id)
        {
            var chosen = await _currencies.GetLiveAsync(id, cancellationToken);
            if (chosen is not null) return chosen;
        }

        var euro = await _currencies.GetBaseCurrencyAsync(cancellationToken);

        // No euro row configured at all: fall back to a synthetic 1:1 base so pricing never
        // fails outright over reference data. Should not happen once the seed has run.
        return euro ?? BaseCurrency();
    }

    private static Currency BaseCurrency() =>
        new() { Code = DomainConstants.BaseCurrencyCode, Name = "Euro", RateToEur = 1m };

    // Converts the amount into the rule's currency, then: percentage part (rounded to 4dp) + fixed
    // amount, raised to the minimum fee, final fee rounded to 2dp. Banker's rounding throughout.
    // Every native figure is also converted to euro at the rule's rate.
    private static CommissionCalculation Calculate(
        CommissionRule rule, decimal inputAmount, Currency inputCurrency, bool isFallback)
    {
        var ruleCurrency = rule.Currency ?? BaseCurrency();

        // Convert the entered amount into the rule's currency, pivoting through euro:
        // eur = amount * inputRate; then amount_in_rule = eur / ruleRate.
        var amount = ruleCurrency.CurrencyId == inputCurrency.CurrencyId
            ? inputAmount
            : inputAmount * inputCurrency.RateToEur / ruleCurrency.RateToEur;

        var percentagePart = Math.Round(
            amount * rule.PercentageRate / 100m, 4, MidpointRounding.ToEven);
        var rawFee = percentagePart + rule.FixedAmount;
        var floored = Math.Max(rawFee, rule.MinimumFee);
        var fee = Math.Round(floored, 2, MidpointRounding.ToEven);

        var amount2 = Math.Round(amount, 2, MidpointRounding.ToEven);
        var rawFee2 = Math.Round(rawFee, 2, MidpointRounding.ToEven);

        decimal ToEur(decimal value) =>
            Math.Round(value * ruleCurrency.RateToEur, 2, MidpointRounding.ToEven);

        return new CommissionCalculation
        {
            AppliedRuleId = rule.CommissionRuleId,
            AppliedRuleName = rule.RuleName,
            IsFallback = isFallback,
            InputAmount = inputAmount,
            InputCurrencyCode = inputCurrency.Code,
            Amount = amount2,
            CurrencyCode = ruleCurrency.Code,
            EurRate = ruleCurrency.RateToEur,
            PercentageRate = rule.PercentageRate,
            FixedAmount = rule.FixedAmount,
            MinimumFee = rule.MinimumFee,
            RawFee = rawFee2,
            Fee = fee,
            MinimumApplied = floored > rawFee,
            AmountEur = ToEur(amount2),
            FixedAmountEur = ToEur(rule.FixedAmount),
            MinimumFeeEur = ToEur(rule.MinimumFee),
            RawFeeEur = ToEur(rawFee2),
            FeeEur = ToEur(fee),
            Reason = Reason(rule, isFallback)
        };
    }

    private static string Reason(CommissionRule rule, bool isFallback)
    {
        if (isFallback)
        {
            return $"No matching rule; fell back to the default '{rule.RuleName}'.";
        }

        var c = rule.RuleCriteria.FirstOrDefault();

        var matched = new List<string>();
        var wildcard = new List<string>();
        (c?.CardSchemeId is null ? wildcard : matched).Add("scheme");
        (c?.ProductTypeId is null ? wildcard : matched).Add("product");
        (c?.FundingTypeId is null ? wildcard : matched).Add("funding");
        (c?.RegionId is null ? wildcard : matched).Add("region");

        var parts = matched.Count > 0
            ? $"matched on {Join(matched)}"
            : "matched every card (all fields are wildcards)";

        if (wildcard.Count > 0 && matched.Count > 0)
        {
            parts += $"; {Join(wildcard)} {(wildcard.Count == 1 ? "was a wildcard" : "were wildcards")}";
        }

        return $"Rule '{rule.RuleName}' selected: {parts}.";
    }

    private static string Join(IReadOnlyList<string> values) => values.Count switch
    {
        1 => values[0],
        2 => $"{values[0]} and {values[1]}",
        _ => $"{string.Join(", ", values.Take(values.Count - 1))} and {values[^1]}"
    };
}
