using System;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Infrastructure.Helper;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Helper;

public class ExcelHelperDateTimeTests
{
    [Theory]
    [InlineData("10/06/26 04:12 PM", 2026, 10, 6, 16, 12, 0)]
    [InlineData("10/06/26 04:14 PM", 2026, 10, 6, 16, 14, 0)]
    [InlineData("10/06/2026 04:12 PM", 2026, 10, 6, 16, 12, 0)]
    [InlineData("10/06/2026, 04:12 PM", 2026, 10, 6, 16, 12, 0)]
    [InlineData("10/06/2026 16:12:00", 2026, 10, 6, 16, 12, 0)]
    [InlineData("07/10/2025, 14:30:00", 2025, 7, 10, 14, 30, 0)]
    [InlineData("07/10/2025 14:30", 2025, 7, 10, 14, 30, 0)]
    [InlineData("07/10/2025, 02:30 PM", 2025, 7, 10, 14, 30, 0)]
    [InlineData("07/10/2025, 02:30:00 AM", 2025, 7, 10, 2, 30, 0)]
    [InlineData("2026-10-06 16:12:00", 2026, 10, 6, 16, 12, 0)]
    [InlineData("2026-10-06T16:12:00Z", 2026, 10, 6, 16, 12, 0)]
    public void ParseDateTime_ValidFormats_ParsesExpectedDateAndComponents(
        string input, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
    {
        var result = ExcelHelper.ParseDateTime(input);

        Assert.Equal(expectedYear, result.Year);
        Assert.Equal(expectedMonth, result.Month);
        Assert.Equal(expectedDay, result.Day);
        Assert.Equal(expectedHour, result.Hour);
        Assert.Equal(expectedMinute, result.Minute);
        Assert.Equal(expectedSecond, result.Second);
    }

    [Fact]
    public void ParseDateTime_SOPMonthFirstPriority_InterpretsMonthFirst()
    {
        // SOP mandates MM/dd/yyyy: 07/10/2025 must be July 10, not October 7
        var result = ExcelHelper.ParseDateTime("07/10/2025");

        Assert.Equal(2025, result.Year);
        Assert.Equal(7, result.Month);
        Assert.Equal(10, result.Day);
    }

    [Fact]
    public void ParseDateTime_WhenEmptyOrWhitespace_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ExcelHelper.ParseDateTime(""));
        Assert.Throws<FormatException>(() => ExcelHelper.ParseDateTime("   "));
    }

    [Fact]
    public void ParseDateTime_EndToEndPhilippineTimeConversion_ProducesExpectedUtcInstant()
    {
        // Raw Excel data from field in Albay: 10/06/26 04:12 PM (PHT, UTC+8)
        var parsedLocal = ExcelHelper.ParseDateTime("10/06/26 04:12 PM");
        var utcInstant = TimeZoneHelper.ConvertPhilippineTimeToUtc(parsedLocal);

        // 4:12 PM (16:12) in PHT is 8:12 AM (08:12) in UTC
        Assert.Equal(DateTimeKind.Utc, utcInstant.Kind);
        Assert.Equal(2026, utcInstant.Year);
        Assert.Equal(10, utcInstant.Month);
        Assert.Equal(6, utcInstant.Day);
        Assert.Equal(8, utcInstant.Hour);
        Assert.Equal(12, utcInstant.Minute);

        // When converted back to Philippine Time (by browser / client), matches raw data
        var backToLocal = TimeZoneHelper.ConvertUtcToPhilippineTime(utcInstant);
        Assert.Equal(16, backToLocal.Hour);
        Assert.Equal(12, backToLocal.Minute);
        Assert.Equal(6, backToLocal.Day);
    }
}
