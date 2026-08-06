namespace BinTool.Core.Models.Audit;

/// <summary>
/// Which commission rule was the configured fallback default at one moment, as stored in an
/// audit entry. Recorded when the default is assigned, moved or cleared - the rule id and
/// its name so the trail reads without a join. A null id means "no default configured".
/// </summary>
public sealed record DefaultRuleSnapshot(int? CommissionRuleId, string? RuleName);
