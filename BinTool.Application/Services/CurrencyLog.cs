using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

internal static partial class CurrencyLog
{
    [LoggerMessage(EventId = 1201, Level = LogLevel.Information,
        Message = "Currency {CurrencyId} {Code} created by {User} at {RateToEur} to the euro.")]
    public static partial void Created(
        ILogger logger, int currencyId, string code, decimal rateToEur, string user);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Information,
        Message = "Currency {CurrencyId} {Code} updated by {User}.")]
    public static partial void Updated(ILogger logger, int currencyId, string code, string user);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information,
        Message = "Currency {CurrencyId} {Code} deleted by {User}.")]
    public static partial void Deleted(ILogger logger, int currencyId, string code, string user);

    [LoggerMessage(EventId = 1204, Level = LogLevel.Information,
        Message = "Currency {CurrencyId} {Code} restored by {User}.")]
    public static partial void Restored(ILogger logger, int currencyId, string code, string user);

    [LoggerMessage(EventId = 1205, Level = LogLevel.Information,
        Message = "Currency {CurrencyId} {Code} was re-added by {User}, reviving the deleted row.")]
    public static partial void Revived(ILogger logger, int currencyId, string code, string user);

    [LoggerMessage(EventId = 1206, Level = LogLevel.Information,
        Message = "Currency {CurrencyId} {Code} rate moved from {PreviousRate} to {NewRate} by {User}; " +
                  "every rule priced in it is repriced.")]
    public static partial void RateChanged(
        ILogger logger, int currencyId, string code,
        decimal previousRate, decimal newRate, string user);

    [LoggerMessage(EventId = 1301, Level = LogLevel.Warning,
        Message = "Currency write refused for {CurrencyId} as {Status}: {Reason}")]
    public static partial void WriteRefused(
        ILogger logger, int currencyId, string status, string reason);
}
