using BinTool.Core.Entities;
using BinTool.Core.Models.Commission;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// Selects the applicable commission rule and works out the fee. Selection is deterministic:
/// among the rules that match the card and are valid on the date, the most specific one wins,
/// with a total ordering so the same inputs always produce the same rule regardless of the
/// order rules were entered. When nothing matches, the configured default is used and the
/// result is flagged as a fallback.
/// </summary>
public class CommissionResolver : ICommissionResolver
{
    private readonly AppDbContext _db;

    public CommissionResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommissionCalculation?> ResolveAsync(
        int cardSchemeId,
        int productTypeId,
        int regionId,
        decimal amount,
        DateTime onDate,
        CancellationToken cancellationToken = default)
    {
        var day = onDate.Date;

        // Every live rule that matches the card's attributes (a null criteria field is a
        // wildcard) and is valid on the day. The set is small, so the tiebreak is settled
        // in memory where it reads clearly.
        var matches = await _db.CommissionRules.AsNoTracking()
            .Include(r => r.RuleCriteria)
            .Where(r => !r.IsDeleted && r.IsActive)
            .Where(r => r.ValidFrom <= day && (r.ValidTo == null || r.ValidTo >= day))
            .Where(r => r.RuleCriteria.Any(c =>
                (c.CardSchemeId == null || c.CardSchemeId == cardSchemeId)
                && (c.ProductTypeId == null || c.ProductTypeId == productTypeId)
                && (c.RegionId == null || c.RegionId == regionId)))
            .ToListAsync(cancellationToken);

        var winner = matches
            .OrderByDescending(r => Specificity(r))     // fewest wildcards wins
            .ThenByDescending(r => r.Priority)          // manual tiebreak
            .ThenByDescending(r => r.ValidFrom)         // newer tariff wins
            .ThenBy(r => r.CommissionRuleId)            // final deterministic tiebreak
            .FirstOrDefault();

        if (winner is not null)
        {
            return Calculate(winner, amount, isFallback: false);
        }

        // No rule matched. Fall back to the configured default, if one is set.
        var defaultRuleId = await _db.DefaultRules.AsNoTracking()
            .Select(d => (int?)d.CommissionRuleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultRuleId is not { } ruleId) return null;

        var fallback = await _db.CommissionRules.AsNoTracking()
            .Include(r => r.RuleCriteria)
            .FirstOrDefaultAsync(r => r.CommissionRuleId == ruleId, cancellationToken);

        // A default that points at a soft-deleted rule is treated as no default at all.
        if (fallback is null || fallback.IsDeleted) return null;

        return Calculate(fallback, amount, isFallback: true);
    }

    /// <summary>
    /// percentage part (rounded to 4dp) + fixed amount, then raised to the minimum fee, with
    /// the final fee rounded to 2dp. Banker's rounding throughout, applied consistently.
    /// </summary>
    private static CommissionCalculation Calculate(CommissionRule rule, decimal amount, bool isFallback)
    {
        var percentagePart = Math.Round(
            amount * rule.PercentageRate / 100m, 4, MidpointRounding.ToEven);
        var rawFee = percentagePart + rule.FixedAmount;
        var floored = Math.Max(rawFee, rule.MinimumFee);
        var fee = Math.Round(floored, 2, MidpointRounding.ToEven);

        return new CommissionCalculation
        {
            AppliedRuleId = rule.CommissionRuleId,
            AppliedRuleName = rule.RuleName,
            IsFallback = isFallback,
            Amount = amount,
            PercentageRate = rule.PercentageRate,
            FixedAmount = rule.FixedAmount,
            MinimumFee = rule.MinimumFee,
            RawFee = Math.Round(rawFee, 2, MidpointRounding.ToEven),
            Fee = fee,
            MinimumApplied = floored > rawFee,
            Reason = Reason(rule, isFallback)
        };
    }

    private static int Specificity(CommissionRule rule)
    {
        var c = rule.RuleCriteria.FirstOrDefault();
        if (c is null) return 0;

        return (c.CardSchemeId is null ? 0 : 1)
            + (c.ProductTypeId is null ? 0 : 1)
            + (c.RegionId is null ? 0 : 1);
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
