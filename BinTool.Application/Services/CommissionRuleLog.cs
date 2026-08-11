using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

/// <summary>
/// The log events commission-rule administration emits, declared once so every call site
/// spells the same event the same way. The generator turns each of these into a strongly
/// typed method with no boxing and no string formatting unless the level is enabled, which
/// is the whole reason for writing them out rather than calling <c>LogInformation</c> inline.
/// <para>
/// The message templates are the contract: <c>{RuleId}</c> and the rest arrive at the sink
/// as named fields, so a log search can ask "everything about rule 42" rather than grepping
/// prose. Interpolated strings would collapse them back into text and are never used here.
/// </para>
/// </summary>
internal static partial class CommissionRuleLog
{
    // 1000-1099: successful writes.

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Commission rule {RuleId} '{RuleName}' created by {User} at priority {Priority}/{PriorityScore}.")]
    public static partial void Created(
        ILogger logger, int ruleId, string ruleName, string user, int priority, int priorityScore);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Commission rule {RuleId} '{RuleName}' updated by {User}.")]
    public static partial void Updated(ILogger logger, int ruleId, string ruleName, string user);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information,
        Message = "Commission rule {RuleId} '{RuleName}' deleted by {User}.")]
    public static partial void Deleted(ILogger logger, int ruleId, string ruleName, string user);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information,
        Message = "Commission rule {RuleId} '{RuleName}' restored by {User}.")]
    public static partial void Restored(ILogger logger, int ruleId, string ruleName, string user);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information,
        Message = "Commission rule {RuleId} '{RuleName}' set as the default by {User}.")]
    public static partial void DefaultSet(ILogger logger, int ruleId, string ruleName, string user);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information,
        Message = "The default commission rule was cleared by {User}; unmatched cards now have no fallback.")]
    public static partial void DefaultCleared(ILogger logger, string user);

    // 1100-1199: refusals. Warning, not Error - a refused write is the guard doing its job,
    // and logging it at Error would train whoever watches the dashboard to ignore Error.

    [LoggerMessage(EventId = 1101, Level = LogLevel.Warning,
        Message = "Commission rule write refused as invalid: {Reason}")]
    public static partial void RefusedAsInvalid(ILogger logger, string reason);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Warning,
        Message = "Commission rule write refused: it conflicts with rule {ConflictingRuleId} '{ConflictingRuleName}'. {Reason}")]
    public static partial void RefusedAsConflicting(
        ILogger logger, int conflictingRuleId, string conflictingRuleName, string reason);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Warning,
        Message = "Commission rule {RuleId} was not found, or is not in a state that allows the requested change: {Reason}")]
    public static partial void RefusedAsUnavailable(ILogger logger, int ruleId, string reason);
}
