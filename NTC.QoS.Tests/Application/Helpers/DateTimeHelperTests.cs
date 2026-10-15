using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Helper;
using Xunit;

namespace NTC.QoS.Tests.Application.Helpers;

public class DateTimeHelperTests
{
    #region NormalizeToUtc Tests

    [Fact]
    public void NormalizeToUtc_WhenKindIsUtc_ReturnsSameDateTime()
    {
        var utcDate = new DateTime(2026, 10, 1, 3, 9, 0, DateTimeKind.Utc);
        var result = utcDate.NormalizeToUtc();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(utcDate, result);
    }

    [Fact]
    public void NormalizeToUtc_WhenKindIsLocal_ConvertsToUtc()
    {
        var localDate = new DateTime(2026, 10, 1, 11, 9, 0, DateTimeKind.Local);
        var result = localDate.NormalizeToUtc();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(localDate.ToUniversalTime(), result);
    }

    [Fact]
    public void NormalizeToUtc_WhenKindIsUnspecified_SpecifiesUtcKind()
    {
        var unspecifiedDate = new DateTime(2026, 10, 1, 11, 9, 0, DateTimeKind.Unspecified);
        var result = unspecifiedDate.NormalizeToUtc();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(unspecifiedDate.Ticks, result.Ticks);
    }

    [Fact]
    public void NormalizeToUtc_Nullable_HandlesNullCorrectly()
    {
        DateTime? nullDate = null;
        var result = nullDate.NormalizeToUtc();

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeToUtc_NullableWithValue_NormalizesToUtc()
    {
        DateTime? date = new DateTime(2026, 5, 20, 14, 0, 0, DateTimeKind.Unspecified);
        var result = date.NormalizeToUtc();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(date.Value.Ticks, result.Value.Ticks);
    }

    #endregion

    #region NormalizeFilterStartDate Tests

    [Fact]
    public void NormalizeFilterStartDate_WhenNull_ReturnsNull()
    {
        DateTime? nullDate = null;
        var result = nullDate.NormalizeFilterStartDate();

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeFilterStartDate_WhenSpecified_NormalizesToUtc()
    {
        DateTime? date = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var result = date.NormalizeFilterStartDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(2026, result.Value.Year);
        Assert.Equal(10, result.Value.Month);
        Assert.Equal(1, result.Value.Day);
        Assert.Equal(0, result.Value.Hour);
        Assert.Equal(0, result.Value.Minute);
    }

    [Fact]
    public void NormalizeFilterStartDate_WhenLocalTime_ConvertsToUniversalTime()
    {
        DateTime? localDate = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Local);
        var result = localDate.NormalizeFilterStartDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(localDate.Value.ToUniversalTime(), result.Value);
    }

    #endregion

    #region NormalizeFilterEndDate Boundary Condition Tests

    [Fact]
    public void NormalizeFilterEndDate_WhenNull_ReturnsNull()
    {
        DateTime? nullDate = null;
        var result = nullDate.NormalizeFilterEndDate();

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeFilterEndDate_WhenDateOnlyUnspecified_ExtendsToEndOfDay()
    {
        // Date-only from query string: e.g. "2026-10-01" binds to midnight with Kind=Unspecified
        DateTime? dateOnly = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var result = dateOnly.NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(2026, result.Value.Year);
        Assert.Equal(10, result.Value.Month);
        Assert.Equal(1, result.Value.Day);
        Assert.Equal(23, result.Value.Hour);
        Assert.Equal(59, result.Value.Minute);
        Assert.Equal(59, result.Value.Second);
    }

    [Fact]
    public void NormalizeFilterEndDate_LeapYearBoundary_ExtendsToEndOfFeb29()
    {
        // Leap year boundary: 2024-02-29
        DateTime? leapDay = new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Unspecified);
        var result = leapDay.NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(2024, result.Value.Year);
        Assert.Equal(2, result.Value.Month);
        Assert.Equal(29, result.Value.Day);
        Assert.Equal(23, result.Value.Hour);
        Assert.Equal(59, result.Value.Minute);
        Assert.Equal(59, result.Value.Second);
    }

    [Fact]
    public void NormalizeFilterEndDate_NonLeapYearEndFeb_ExtendsToEndOfFeb28()
    {
        // Non-leap year end of February: 2025-02-28
        DateTime? febEnd = new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Unspecified);
        var result = febEnd.NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(2025, result.Value.Year);
        Assert.Equal(2, result.Value.Month);
        Assert.Equal(28, result.Value.Day);
        Assert.Equal(23, result.Value.Hour);
        Assert.Equal(59, result.Value.Minute);
        Assert.Equal(59, result.Value.Second);
    }

    [Fact]
    public void NormalizeFilterEndDate_YearBoundary_ExtendsToEndOfDec31WithoutSpillingToNextYear()
    {
        // Year boundary: 2026-12-31 should extend to 2026-12-31 23:59:59.9999999 and not 2027
        DateTime? yearEnd = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Unspecified);
        var result = yearEnd.NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(2026, result.Value.Year);
        Assert.Equal(12, result.Value.Month);
        Assert.Equal(31, result.Value.Day);
        Assert.Equal(23, result.Value.Hour);
        Assert.Equal(59, result.Value.Minute);
        Assert.Equal(59, result.Value.Second);
    }

    [Fact]
    public void NormalizeFilterEndDate_MonthBoundary_ExtendsToEndOfMonth()
    {
        // Month boundary for 30-day month: 2026-04-30
        DateTime? monthEnd = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Unspecified);
        var result = monthEnd.NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(2026, result.Value.Year);
        Assert.Equal(4, result.Value.Month);
        Assert.Equal(30, result.Value.Day);
        Assert.Equal(23, result.Value.Hour);
        Assert.Equal(59, result.Value.Minute);
    }

    [Fact]
    public void NormalizeFilterEndDate_WhenExplicitTimeProvided_DoesNotDoubleExtend()
    {
        // UTC ISO from convertRangeToUtcIso: e.g. "2026-10-01T15:59:59.999Z" has explicit time
        var explicitUtcTime = new DateTime(2026, 10, 1, 15, 59, 59, 999, DateTimeKind.Utc);
        var result = ((DateTime?)explicitUtcTime).NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(explicitUtcTime, result.Value);
    }

    [Fact]
    public void NormalizeFilterEndDate_WhenExplicitUtcMidnight_DoesNotExtendToEndOfDay()
    {
        // Explicit UTC midnight (Kind == Utc, TimeOfDay == 0) represents an exact point in time
        var utcMidnight = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = ((DateTime?)utcMidnight).NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(utcMidnight, result.Value);
    }

    [Fact]
    public void NormalizeFilterEndDate_WhenUnspecifiedWithTime_DoesNotExtendToEndOfDay()
    {
        // Time is not zero (e.g. 14:30:00), so it should not be treated as date-only
        var unspecifiedWithTime = new DateTime(2026, 10, 1, 14, 30, 0, DateTimeKind.Unspecified);
        var result = ((DateTime?)unspecifiedWithTime).NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(14, result.Value.Hour);
        Assert.Equal(30, result.Value.Minute);
    }

    [Fact]
    public void NormalizeFilterEndDate_WhenLocalTime_ConvertsToUniversalTime()
    {
        var localTime = new DateTime(2026, 10, 1, 18, 0, 0, DateTimeKind.Local);
        var result = ((DateTime?)localTime).NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(localTime.ToUniversalTime(), result.Value);
    }

    #endregion

    #region Downstream Consumer Compatibility Tests

    [Theory]
    [InlineData("2026-10-01T15:59:59.999Z", true)]
    [InlineData("2026-10-01T00:00:00.000Z", true)]
    [InlineData("2026-10-01", false)]
    public void DownstreamApiConsumer_FilterDateParsingSimulation(string inputString, bool isIsoWithZ)
    {
        // Simulate ASP.NET Core model binder receiving date query strings from external clients
        DateTime parsedDate = DateTime.Parse(inputString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (isIsoWithZ)
        {
            Assert.Equal(DateTimeKind.Utc, parsedDate.Kind);

            // Filter start date keeps exact instant
            var normalizedStart = ((DateTime?)parsedDate).NormalizeFilterStartDate();
            Assert.Equal(parsedDate, normalizedStart);

            // Filter end date preserves exact instant without extra day addition
            var normalizedEnd = ((DateTime?)parsedDate).NormalizeFilterEndDate();
            Assert.Equal(parsedDate, normalizedEnd);
        }
        else
        {
            // Date-only input: "YYYY-MM-DD" binds as midnight with Kind=Unspecified
            Assert.Equal(DateTimeKind.Unspecified, parsedDate.Kind);

            var normalizedStart = ((DateTime?)parsedDate).NormalizeFilterStartDate();
            Assert.Equal(DateTimeKind.Utc, normalizedStart!.Value.Kind);
            Assert.Equal(0, normalizedStart.Value.Hour);

            var normalizedEnd = ((DateTime?)parsedDate).NormalizeFilterEndDate();
            Assert.Equal(DateTimeKind.Utc, normalizedEnd!.Value.Kind);
            // End of day is covered so user query includes all tests on that day
            Assert.Equal(23, normalizedEnd.Value.Hour);
            Assert.Equal(59, normalizedEnd.Value.Minute);
            Assert.Equal(parsedDate.Day, normalizedEnd.Value.Day);
        }
    }

    [Fact]
    public void DownstreamApiConsumer_PostgreSqlTimestamptzCompatibility()
    {
        // Npgsql 6+ requires DateTimeKind.Utc when writing to timestamptz columns
        // Verify NormalizeToUtc produces Kind == Utc for all potential inputs
        var inputs = new[]
        {
            DateTime.UtcNow,
            DateTime.Now,
            new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Unspecified)
        };

        foreach (var input in inputs)
        {
            var normalized = input.NormalizeToUtc();
            Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        }
    }

    #endregion

    #region Date Range Resolution Tests

    [Fact]
    public void GetPastDaysRange_PositiveDays_ReturnsExpectedRange()
    {
        var (startDateUtc, endDateUtc) = DateTimeHelper.GetPastDaysRange(7);

        Assert.Equal(DateTimeKind.Utc, startDateUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, endDateUtc.Kind);
        Assert.Equal(7, (endDateUtc - startDateUtc).TotalDays);
        Assert.Equal(TimeSpan.Zero, endDateUtc.TimeOfDay);
        Assert.Equal(TimeSpan.Zero, startDateUtc.TimeOfDay);
    }

    [Fact]
    public void GetPastDaysRange_NegativeDays_TreatsAsPositive()
    {
        var (startDateUtc, endDateUtc) = DateTimeHelper.GetPastDaysRange(-7);

        Assert.Equal(7, (endDateUtc - startDateUtc).TotalDays);
    }

    [Fact]
    public void GetPastDaysRange_ZeroDays_ReturnsSameDates()
    {
        var (startDateUtc, endDateUtc) = DateTimeHelper.GetPastDaysRange(0);

        Assert.Equal(startDateUtc, endDateUtc);
    }

    [Fact]
    public void GetUnlimitedDateRange_ReturnsNullTuple()
    {
        var (startDate, endDate) = DateTimeHelper.GetUnlimitedDateRange();

        Assert.Null(startDate);
        Assert.Null(endDate);
    }

    [Fact]
    public void ResolveDateRange_WhenBothDatesProvided_ReturnsUtcDates()
    {
        var loggerMock = new Mock<ILogger>();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var end = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Local);

        var (finalStart, finalEnd) = DateTimeHelper.ResolveDateRange(start, end, loggerMock.Object);

        Assert.NotNull(finalStart);
        Assert.NotNull(finalEnd);
        Assert.Equal(DateTimeKind.Utc, finalStart.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, finalEnd.Value.Kind);
        Assert.Equal(start.ToUniversalTime(), finalStart.Value);
        Assert.Equal(end.ToUniversalTime(), finalEnd.Value);
    }

    [Fact]
    public void ResolveDateRange_WhenOnlyDaysInPastProvided_ReturnsCalculatedRange()
    {
        var loggerMock = new Mock<ILogger>();

        var (finalStart, finalEnd) = DateTimeHelper.ResolveDateRange(null, null, loggerMock.Object, daysInPast: 14);

        Assert.NotNull(finalStart);
        Assert.NotNull(finalEnd);
        Assert.Equal(14, (finalEnd.Value - finalStart.Value).TotalDays);
    }

    [Fact]
    public void ResolveDateRange_WhenNeitherProvided_ReturnsUnlimitedRange()
    {
        var loggerMock = new Mock<ILogger>();

        var (finalStart, finalEnd) = DateTimeHelper.ResolveDateRange(null, null, loggerMock.Object);

        Assert.Null(finalStart);
        Assert.Null(finalEnd);
    }

    [Fact]
    public void ResolveDateRange_WhenOnlyStartDateProvided_FallsBackToUnlimited()
    {
        var loggerMock = new Mock<ILogger>();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var (finalStart, finalEnd) = DateTimeHelper.ResolveDateRange(start, null, loggerMock.Object);

        Assert.Null(finalStart);
        Assert.Null(finalEnd);
    }

    [Fact]
    public void ResolveDateRange_WhenOnlyEndDateProvided_FallsBackToUnlimited()
    {
        var loggerMock = new Mock<ILogger>();
        var end = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        var (finalStart, finalEnd) = DateTimeHelper.ResolveDateRange(null, end, loggerMock.Object);

        Assert.Null(finalStart);
        Assert.Null(finalEnd);
    }

    #endregion

    #region ConvertPhilippineTimeToUtc Tests

    [Fact]
    public void ConvertPhilippineTimeToUtc_WhenUnspecifiedPhtAfternoon_SubtractsEightHours()
    {
        // 4:12 PM on Oct 6 in PHT (UTC+8) -> 8:12 AM on Oct 6 in UTC
        var phtAfternoon = new DateTime(2026, 10, 6, 16, 12, 0, DateTimeKind.Unspecified);
        var result = phtAfternoon.ConvertPhilippineTimeToUtc();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(2026, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(6, result.Day);
        Assert.Equal(8, result.Hour);
        Assert.Equal(12, result.Minute);
    }

    [Fact]
    public void ConvertPhilippineTimeToUtc_WhenUnspecifiedPhtMorning_CrossesDayBoundaryBackwards()
    {
        // 4:00 AM on Oct 6 in PHT (UTC+8) -> 8:00 PM on Oct 5 in UTC
        var phtMorning = new DateTime(2026, 10, 6, 4, 0, 0, DateTimeKind.Unspecified);
        var result = phtMorning.ConvertPhilippineTimeToUtc();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(2026, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(5, result.Day);
        Assert.Equal(20, result.Hour);
        Assert.Equal(0, result.Minute);
    }

    [Fact]
    public void ConvertPhilippineTimeToUtc_WhenAlreadyUtc_ReturnsUnchanged()
    {
        var utcDate = new DateTime(2026, 10, 6, 8, 12, 0, DateTimeKind.Utc);
        var result = utcDate.ConvertPhilippineTimeToUtc();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(utcDate, result);
    }

    [Fact]
    public void ConvertPhilippineTimeToUtc_Nullable_HandlesNullCorrectly()
    {
        DateTime? nullDate = null;
        var result = nullDate.ConvertPhilippineTimeToUtc();

        Assert.Null(result);
    }

    #endregion
}


