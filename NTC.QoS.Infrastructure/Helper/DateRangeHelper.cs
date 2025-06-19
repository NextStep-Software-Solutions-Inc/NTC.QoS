using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Helper;

public class DateRangeHelper : IDateRangeHelper
{
    /// <summary>
    /// Calculates a default date range in UTC (e.g., last 30 days ending yesterday).
    /// This is the primary default used if no specific range method is called.
    /// </summary>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    public (DateTime startDateUtc, DateTime endDateUtc) GetDefaultDateRange()
    {
        // Defaulting to the last 30 full days, ending at the start of the current UTC day.
        // This avoids including incomplete data for the current day.
        return GetPastDaysRange(30);
    }

    /// <summary>
    /// Calculates a date range for a specified number of full days in the past, ending yesterday.
    /// </summary>
    /// <param name="daysInPast">The number of days in the past to include.</param>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    public (DateTime startDateUtc, DateTime endDateUtc) GetPastDaysRange(int daysInPast)
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime endDateUtc = nowUtc.Date; // End of yesterday (start of today UTC)
        DateTime startDateUtc = endDateUtc.AddDays(-daysInPast);

        return (startDateUtc, endDateUtc);
    }

    /// <summary>
    /// Calculates the date range for the current calendar month in UTC.
    /// </summary>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    public (DateTime startDateUtc, DateTime endDateUtc) GetCurrentMonthRange()
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime startDateUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime endDateUtc = startDateUtc.AddMonths(1).AddSeconds(-1); // End of the current month

        return (startDateUtc, endDateUtc);
    }

    // Add other specific date range calculations here if needed, e.g.:
    /*
    public (DateTime startDateUtc, DateTime endDateUtc) GetCurrentYearRange()
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime startDateUtc = new DateTime(nowUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime endDateUtc = startDateUtc.AddYears(1).AddSeconds(-1);
        return (startDateUtc, endDateUtc);
    }
    */
}