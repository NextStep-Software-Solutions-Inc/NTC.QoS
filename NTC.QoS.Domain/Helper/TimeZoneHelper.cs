namespace NTC.QoS.Domain.Helper;

/// <summary>
/// Provides cross-platform timezone utilities for Philippine Standard Time (PST / PHT, UTC+8).
/// Handles Windows ("Singapore Standard Time"), Linux/Docker ("Asia/Manila"), and custom UTC+8 fallbacks.
/// </summary>
public static class TimeZoneHelper
{
    private static readonly Lazy<TimeZoneInfo> _philippineTimeZone = new(ResolvePhilippineTimeZone);

    /// <summary>
    /// Gets the Philippine Standard Time (PST / PHT, UTC+8) TimeZoneInfo.
    /// Does not observe Daylight Saving Time (fixed UTC+8 offset).
    /// </summary>
    public static TimeZoneInfo PhilippineTimeZone => _philippineTimeZone.Value;

    /// <summary>
    /// Converts a DateTime assumed to be in Philippine Standard Time (UTC+8) into UTC.
    /// If Kind is already Utc, it is returned unchanged.
    /// If Kind is Unspecified or Local, it is treated as Philippine local time and converted to UTC.
    /// </summary>
    /// <param name="dateTime">The timestamp to convert.</param>
    /// <returns>A DateTime in UTC (Kind = Utc).</returns>
    public static DateTime ConvertPhilippineTimeToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }

        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, PhilippineTimeZone);
    }

    /// <summary>
    /// Converts a UTC DateTime to Philippine Standard Time (UTC+8).
    /// </summary>
    /// <param name="utcDateTime">The UTC timestamp to convert.</param>
    /// <returns>A DateTime in Philippine Standard Time.</returns>
    public static DateTime ConvertUtcToPhilippineTime(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, PhilippineTimeZone);
    }

    private static TimeZoneInfo ResolvePhilippineTimeZone()
    {
        try
        {
            // Standard IANA identifier (Linux / Docker / macOS / .NET 6+)
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Windows identifier fallback
                return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback fixed +08:00 offset if neither system timezone ID is found
                return TimeZoneInfo.CreateCustomTimeZone(
                    "PHT",
                    TimeSpan.FromHours(8),
                    "(UTC+08:00) Philippine Standard Time",
                    "Philippine Standard Time");
            }
        }
    }
}
