using System.Text;
using NTC.QoS.Infrastructure.Helper;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Helper;

public class ImportErrorLoggerTests
{
    [Fact]
    public void AddError_TracksErrorsCorrectly()
    {
        var logger = new ImportErrorLogger();

        Assert.False(logger.HasErrors);
        Assert.Equal(0, logger.ErrorCount);

        logger.AddError(2, "Provider", "UNKNOWN_ISP", "Provider not recognized", "Select SMART or GLOBE");

        Assert.True(logger.HasErrors);
        Assert.Equal(1, logger.ErrorCount);
        Assert.Equal(2, logger.Errors[0].RowNumber);
        Assert.Equal("Provider", logger.Errors[0].Column);
        Assert.Equal("UNKNOWN_ISP", logger.Errors[0].Value);
        Assert.Equal("Provider not recognized", logger.Errors[0].ErrorMessage);
        Assert.Equal("Select SMART or GLOBE", logger.Errors[0].SuggestedResolution);
    }

    [Fact]
    public void GenerateSummary_ReturnsUserFriendlySummaryWithSamples()
    {
        var logger = new ImportErrorLogger();
        logger.AddError(4, "Provider", "ABC", "Unknown provider", "Select SMART, GLOBE, or DITO");
        logger.AddError(5, "Azimuth", "invalid", "Non-numeric azimuth", "Enter decimal value");
        logger.AddError(6, "Latitude", "999", "Out of range", "Must be between -90 and 90");

        var summary = logger.GenerateSummary(maxSampleErrors: 2);

        Assert.Contains("Found 3 invalid record(s)", summary);
        Assert.Contains("Row 4 [Provider]", summary);
        Assert.Contains("Row 5 [Azimuth]", summary);
        Assert.Contains("+1 more. Download error report for full resolution steps.", summary);
    }

    [Fact]
    public void GenerateCsvBytes_IncludesUtf8BomAndRfc4180Escaping()
    {
        var logger = new ImportErrorLogger();
        logger.AddError(10, "Provider", "SMART, INC.", "Provider with comma, \"quotes\", and newline\ntest", "Check guidelines");

        var bytes = logger.GenerateCsvBytes();

        // Check UTF-8 BOM (\uFEFF)
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        var content = Encoding.UTF8.GetString(bytes);
        Assert.Contains("Row Number,Column,Invalid Value,Error Description,Suggested Resolution", content);
        Assert.Contains("10,\"Provider\",\"SMART, INC.\",\"Provider with comma, \"\"quotes\"\", and newline\ntest\",\"Check guidelines\"", content);
    }
}
