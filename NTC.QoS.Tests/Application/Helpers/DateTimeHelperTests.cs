using System;
using NTC.QoS.Application.Helper;
using Xunit;

namespace NTC.QoS.Tests.Application.Helpers;

public class DateTimeHelperTests
{
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
    }

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
    public void NormalizeFilterEndDate_WhenExplicitTimeProvided_DoesNotDoubleExtend()
    {
        // UTC ISO from convertRangeToUtcIso: e.g. "2026-10-01T15:59:59.999Z" has explicit time
        var explicitUtcTime = new DateTime(2026, 10, 1, 15, 59, 59, 999, DateTimeKind.Utc);
        var result = ((DateTime?)explicitUtcTime).NormalizeFilterEndDate();

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(explicitUtcTime, result.Value);
    }
}
