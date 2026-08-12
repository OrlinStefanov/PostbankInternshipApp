using BinTool.Application.Models.Commission;
using BinTool.Domain.Entities;

namespace BinTool.Application.Mapping;

public static class CommissionCalculationMapper
{
    public static CommissionCalculation From(
        CommissionRule rule, Currency ruleCurrency, Currency inputCurrency,
        decimal inputAmount, decimal amount, decimal rawFee, decimal fee,
        bool minimumApplied, bool isFallback, string reason)
    {
        decimal ToEur(decimal value) =>
            Math.Round(value * ruleCurrency.RateToEur, 2, MidpointRounding.ToEven);

        return new CommissionCalculation
        {
            AppliedRuleId = rule.CommissionRuleId,
            AppliedRuleName = rule.RuleName,
            IsFallback = isFallback,
            InputAmount = inputAmount,
            InputCurrencyCode = inputCurrency.Code,
            Amount = amount,
            CurrencyCode = ruleCurrency.Code,
            EurRate = ruleCurrency.RateToEur,
            PercentageRate = rule.PercentageRate,
            FixedAmount = rule.FixedAmount,
            MinimumFee = rule.MinimumFee,
            RawFee = rawFee,
            Fee = fee,
            MinimumApplied = minimumApplied,
            AmountEur = ToEur(amount),
            FixedAmountEur = ToEur(rule.FixedAmount),
            MinimumFeeEur = ToEur(rule.MinimumFee),
            RawFeeEur = ToEur(rawFee),
            FeeEur = ToEur(fee),
            Reason = reason
        };
    }
}
