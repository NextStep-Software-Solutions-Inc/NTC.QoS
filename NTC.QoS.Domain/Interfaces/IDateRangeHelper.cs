namespace NTC.QoS.Domain.Interfaces;

public interface IDateRangeHelper
{
    /// <summary>
    /// Calculates a default date range (start and end) in UTC.
    /// </summary>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    (DateTime startDateUtc, DateTime endDateUtc) GetDefaultDateRange();

    /// <summary>
    /// Calculates a default date range (start and end) based on a specified number of days in the past.
    /// </summary>
    /// <param name="daysInPast">The number of days in the past to calculate the range from the current date (excluding the current partial day).</param>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    (DateTime startDateUtc, DateTime endDateUtc) GetPastDaysRange(int daysInPast);

    /// <summary>
    /// Calculates a default date range (start and end) for the current month.
    /// </summary>
    /// <returns>A tuple containing the UTC start date and UTC end date.</returns>
    (DateTime startDateUtc, DateTime endDateUtc) GetCurrentMonthRange();

    // You can add more methods here as your date range needs grow
    // (DateTime startDateUtc, DateTime endDateUtc) GetCurrentQuarterRange();
    // (DateTime startDateUtc, DateTime endDateUtc) GetCurrentYearRange();
}
