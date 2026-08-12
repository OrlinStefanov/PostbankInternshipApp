using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

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
