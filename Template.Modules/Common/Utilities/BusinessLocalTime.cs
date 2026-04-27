namespace Template.Modules.Common.Utilities;

/// <summary>
/// Converts schedule wall-clock times (date + time-of-day) using an IANA timezone to UTC instants.
/// </summary>
public static class BusinessLocalTime
{
    public const string DefaultIanaTimeZoneId = "Asia/Karachi";

    public static TimeZoneInfo ResolveTimeZoneInfo(string? ianaId)
    {
        var id = string.IsNullOrWhiteSpace(ianaId) ? DefaultIanaTimeZoneId : ianaId.Trim();
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultIanaTimeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultIanaTimeZoneId);
        }
    }

    /// <summary>
    /// Interprets <paramref name="localTimeOfDay"/> on <paramref name="date"/> in <paramref name="zone"/> and returns the same instant in UTC.
    /// </summary>
    public static DateTimeOffset ToUtcInstant(DateOnly date, TimeOnly localTimeOfDay, TimeZoneInfo zone)
    {
        var localUnspecified = date.ToDateTime(localTimeOfDay, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localUnspecified, zone);
    }

    public static DateOnly GetDateInZone(DateTimeOffset instant, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(instant.UtcDateTime, zone);
        return DateOnly.FromDateTime(local);
    }
}
