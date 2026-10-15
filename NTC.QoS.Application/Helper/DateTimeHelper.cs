using Microsoft.Extensions.Logging;

namespace NTC.QoS.Application.Helper;

public static class DateTimeHelper
{
    /// <summary>
    /// Calculates a date range for a specified number of full days in the past, ending yesterday.
    /// </summary>
    /// <param name="daysInPast">The number of days in the past to include.</param>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    public static (DateTime startDateUtc, DateTime endDateUtc) GetPastDaysRange(int daysInPast)
    {
        // Ensure daysInPast is non-negative to avoid inverted ranges.
        // If a negative value is passed, treat it as its positive equivalent.
        if (daysInPast < 0)
        {
            // You might choose to log a warning here if a negative value is considered unusual input.
            daysInPast = Math.Abs(daysInPast);
        }

        DateTime nowUtc = DateTime.UtcNow;
        DateTime endDateUtc = nowUtc.Date; // End of yesterday (start of today UTC). Example: if now is 2025-06-26 10:50, endDateUtc is 2025-06-26 00:00.
        DateTime startDateUtc = endDateUtc.AddDays(-daysInPast);

        return (startDateUtc, endDateUtc);
    }

    /// <summary>
    /// Returns a date range representing the absolute minimum and maximum possible dates in UTC.
    /// This signifies no specific date limitation.
    /// </summary>
    /// <returns>A tuple containing DateTime.MinValueUtc and DateTime.MaxValueUtc.</returns>
    public static (DateTime? startDateUtc, DateTime? endDateUtc) GetUnlimitedDateRange()
    {
        // DateTime.MinValue and DateTime.MaxValue are already effectively UTC in their representation
        // as they don't carry timezone info. ToUniversalTime() on them simply sets the Kind to Utc.
        return (null, null);
    }

    /// <summary>
    /// Resolves the final UTC date range based on provided nullable dates or an optional
    /// number of days in the past.
    /// Precedence: 1. Specific startDate/endDate, 2. daysInPast, 3. Unlimited range.
    /// Logs an informational message about which date range was used.
    /// </summary>
    /// <param name="startDate">The optional start date provided by the caller.</param>
    /// <param name="endDate">The optional end date provided by the caller.</param>
    /// <param name="logger">The logger instance to use for logging informational messages.</param>
    /// <param name="daysInPast">An optional number of days in the past to calculate the range.</param>
    /// <returns>A tuple containing the resolved UTC start date and UTC end date.</returns>
    public static (DateTime? finalStartDateUtc, DateTime? finalEndDateUtc) ResolveDateRange(
        DateTime? startDate,
        DateTime? endDate,
        ILogger logger,
        int? daysInPast = null) // Optional parameter
    {
        DateTime? finalStartDate;
        DateTime? finalEndDate;

        if (startDate.HasValue && endDate.HasValue)
        {
            // Scenario 1: Specific start and end dates are provided
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
            logger.LogInformation("Using provided date range: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }
        else if (daysInPast.HasValue)
        {
            // Scenario 2: Only daysInPast is provided
            // Calculate the range based on the specified number of days in the past
            (finalStartDate, finalEndDate) = GetPastDaysRange(daysInPast.Value);
            logger.LogInformation("No specific date range provided. Using calculated range for {Days} days in the past: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", daysInPast.Value, finalStartDate, finalEndDate);
        }
        else
        {
            // Scenario 3: Neither specific dates nor daysInPast are provided, use unlimited range
            (finalStartDate, finalEndDate) = GetUnlimitedDateRange();
            logger.LogInformation("No date range or days in past provided. Using default unlimited range: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }

        return (finalStartDate, finalEndDate);
    }

    /// <summary>
    /// Normalizes a DateTime to UTC.
    /// Handles DateTimeKind.Utc, DateTimeKind.Local (converts to UTC), and DateTimeKind.Unspecified (assumes UTC).
    /// Prevents subtle day-boundary off-by-one errors when comparing against PostgreSQL timestamptz columns.
    /// </summary>
    public static DateTime NormalizeToUtc(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Normalizes a nullable DateTime to UTC.
    /// </summary>
    public static DateTime? NormalizeToUtc(this DateTime? dateTime)
    {
        return dateTime.HasValue ? dateTime.Value.NormalizeToUtc() : null;
    }

    /// <summary>
    /// Normalizes a filter start date to UTC.
    /// </summary>
    public static DateTime? NormalizeFilterStartDate(this DateTime? startDate)
    {
        if (!startDate.HasValue) return null;
        return startDate.Value.NormalizeToUtc();
    }

    /// <summary>
    /// Normalizes a filter end date to UTC.
    /// If the input has no time component (midnight) and is DateTimeKind.Unspecified (e.g. date-only "YYYY-MM-DD"),
    /// extends it to the end of that day (23:59:59.9999999) to prevent day-boundary exclusion errors.
    /// If it already contains an explicit time component (e.g. from convertRangeToUtcIso), normalizes directly to UTC.
    /// </summary>
    public static DateTime? NormalizeFilterEndDate(this DateTime? endDate)
    {
        if (!endDate.HasValue) return null;

        // If a date-only string was passed (midnight with unspecified kind), include the entire day
        if (endDate.Value.TimeOfDay == TimeSpan.Zero && endDate.Value.Kind == DateTimeKind.Unspecified)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            return DateTime.SpecifyKind(endOfDay, DateTimeKind.Utc);
        }

        return endDate.Value.NormalizeToUtc();
    }

    /// <summary>
    /// Converts a DateTime assumed to be in Philippine Standard Time (UTC+8) into UTC.
    /// Delegates to TimeZoneHelper.ConvertPhilippineTimeToUtc.
    /// </summary>
    public static DateTime ConvertPhilippineTimeToUtc(this DateTime dateTime)
    {
        return NTC.QoS.Domain.Helper.TimeZoneHelper.ConvertPhilippineTimeToUtc(dateTime);
    }

    /// <summary>
    /// Converts a nullable DateTime assumed to be in Philippine Standard Time (UTC+8) into UTC.
    /// </summary>
    public static DateTime? ConvertPhilippineTimeToUtc(this DateTime? dateTime)
    {
        return dateTime.HasValue ? dateTime.Value.ConvertPhilippineTimeToUtc() : null;
    }
}
