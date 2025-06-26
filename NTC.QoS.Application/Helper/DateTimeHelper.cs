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
}