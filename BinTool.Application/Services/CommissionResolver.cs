using BinTool.Application.Abstractions;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Commission;
using BinTool.Domain.Common;
using BinTool.Domain.Entities;

namespace BinTool.Application.Services;

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

        var inputCurrency = await ResolveInputCurrencyAsync(inputCurrencyId, cancellationToken);

        var matches = await _rules.FindMatchingAsync(
            cardSchemeId, productTypeId, fundingTypeId, regionId, day, cancellationToken);

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

    private async Task<Currency> ResolveInputCurrencyAsync(
        int? inputCurrencyId, CancellationToken cancellationToken)
    {
        if (inputCurrencyId is { } id)
        {
            var chosen = await _currencies.GetLiveAsync(id, cancellationToken);
            if (chosen is not null) return chosen;
        }

        var euro = await _currencies.GetBaseCurrencyAsync(cancellationToken);

        return euro ?? BaseCurrency();
    }

    private static Currency BaseCurrency() =>
        new() { Code = DomainConstants.BaseCurrencyCode, Name = "Euro", RateToEur = 1m };

    private static CommissionCalculation Calculate(
        CommissionRule rule, decimal inputAmount, Currency inputCurrency, bool isFallback)
    {
        var ruleCurrency = rule.Currency ?? BaseCurrency();

        var amount = ruleCurrency.CurrencyId == inputCurrency.CurrencyId
            ? inputAmount
            : inputAmount * inputCurrency.RateToEur / ruleCurrency.RateToEur;

        var percentagePart = Math.Round(
            amount * rule.PercentageRate / 100m, 4, MidpointRounding.ToEven);
        var rawFee = percentagePart + rule.FixedAmount;
        var floored = Math.Max(rawFee, rule.MinimumFee);
        var fee = Math.Round(floored, 2, MidpointRounding.ToEven);

        return CommissionCalculationMapper.From(
            rule, ruleCurrency, inputCurrency,
            inputAmount,
            amount: Math.Round(amount, 2, MidpointRounding.ToEven),
            rawFee: Math.Round(rawFee, 2, MidpointRounding.ToEven),
            fee,
            minimumApplied: floored > rawFee,
            isFallback,
            reason: Reason(rule, isFallback));
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
