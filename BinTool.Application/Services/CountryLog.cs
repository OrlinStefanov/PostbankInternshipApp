using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

internal static partial class CountryLog
{
    [LoggerMessage(EventId = 1401, Level = LogLevel.Information,
        Message = "Country {CountryId} {IsoCode} created by {User} in region {RegionName}.")]
    public static partial void Created(
        ILogger logger, int countryId, string isoCode, string regionName, string user);

    [LoggerMessage(EventId = 1402, Level = LogLevel.Information,
        Message = "Country {CountryId} {IsoCode} updated by {User}.")]
    public static partial void Updated(ILogger logger, int countryId, string isoCode, string user);

    [LoggerMessage(EventId = 1403, Level = LogLevel.Information,
        Message = "Country {CountryId} {IsoCode} deleted by {User}.")]
    public static partial void Deleted(ILogger logger, int countryId, string isoCode, string user);

    [LoggerMessage(EventId = 1404, Level = LogLevel.Information,
        Message = "Country {CountryId} {IsoCode} restored by {User}.")]
    public static partial void Restored(ILogger logger, int countryId, string isoCode, string user);

    [LoggerMessage(EventId = 1405, Level = LogLevel.Information,
        Message = "Country {CountryId} {IsoCode} was re-added by {User}, reviving the deleted row.")]
    public static partial void Revived(ILogger logger, int countryId, string isoCode, string user);

    [LoggerMessage(EventId = 1501, Level = LogLevel.Warning,
        Message = "Country write refused for {CountryId} as {Status}: {Reason}")]
    public static partial void WriteRefused(
        ILogger logger, int countryId, string status, string reason);
}
