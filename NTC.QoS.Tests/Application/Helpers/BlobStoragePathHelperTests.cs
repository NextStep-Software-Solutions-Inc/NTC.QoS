using NTC.QoS.Application.Helpers;
using NTC.QoS.Domain.Enums;
using Xunit;

namespace NTC.QoS.Tests.Application.Helpers;

public class BlobStoragePathHelperTests
{
    [Theory]
    [InlineData(null, "region-default")]
    [InlineData("", "region-default")]
    [InlineData("   ", "region-default")]
    [InlineData("!@#$%", "region-default")]
    [InlineData("06", "region-06")]
    [InlineData("PH06", "region-ph06")]
    [InlineData("region-01", "region-01")]
    [InlineData("REGION-01", "region-01")]
    [InlineData("Region VI", "region-regionvi")]
    [InlineData("Region VI (Western Visayas)", "region-regionviwesternvisayas")]
    [InlineData("R-01", "region-r-01")]
    public void SanitizeRegionCode_ReturnsExpectedPathSegment(string? rawCode, string expected)
    {
        var result = BlobStoragePathHelper.SanitizeRegionCode(rawCode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(FileTypeEnum.Speedtest, "region-01", "test.xlsx", "region-01/speedtests/test.xlsx")]
    [InlineData(FileTypeEnum.Cell, "region-01", "test.xlsx", "region-01/cells/test.xlsx")]
    [InlineData(FileTypeEnum.NAP, "region-01", "test.xlsx", "region-01/naps/test.xlsx")]
    [InlineData(FileTypeEnum.Cellular, "region-01", "test.xlsx", "region-01/cellulars/test.xlsx")]
    [InlineData(FileTypeEnum.CellularCoverage, "region-01", "test.xlsx", "region-01/cellular-coverages/test.xlsx")]
    [InlineData(FileTypeEnum.WirelessValidation, "region-01", "test.xlsx", "region-01/wireless-validations/test.xlsx")]
    [InlineData(FileTypeEnum.Unknown, "region-01", "test.xlsx", "region-01/uploads/test.xlsx")]
    public void GetBlobPathWithRegion_ReturnsExpectedPath(FileTypeEnum fileType, string regionCode, string fileName, string expected)
    {
        var result = BlobStoragePathHelper.GetBlobPathWithRegion(fileType, regionCode, fileName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Production", "production")]
    [InlineData("production", "production")]
    [InlineData("staging", "production")]
    [InlineData("Staging", "production")]
    [InlineData("Development", "development")]
    [InlineData("development", "development")]
    [InlineData("Test", "development")]
    [InlineData(null, "development")]
    public void GetContainerName_ReturnsExpectedContainer(string? environment, string expected)
    {
        var result = BlobStoragePathHelper.GetContainerName(environment);
        Assert.Equal(expected, result);
    }
}
