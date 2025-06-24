namespace NTC.QoS.Application.Helper;

public static class DateTimeHelper
{
    /// <summary>
    /// Calculates a default date range in UTC (e.g., last 30 days ending yesterday).
    /// This is the primary default used if no specific range method is called.
    /// </summary>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    public static (DateTime startDateUtc, DateTime endDateUtc) GetDefaultDateRange()
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
    public static (DateTime startDateUtc, DateTime endDateUtc) GetPastDaysRange(int daysInPast)
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime endDateUtc = nowUtc.Date; // End of yesterday (start of today UTC)
        DateTime startDateUtc = endDateUtc.AddDays(-daysInPast);

        return (startDateUtc, endDateUtc);
    }
}
