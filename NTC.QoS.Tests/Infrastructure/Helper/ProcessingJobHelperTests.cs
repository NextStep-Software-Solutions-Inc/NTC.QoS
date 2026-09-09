using System;
using NTC.QoS.Infrastructure.Helper;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Helper;

public class ProcessingJobHelperTests
{
    [Theory]
    [InlineData("http://localhost:10000/dev_storage_node/development/region-ph06/speedtests/test.xlsx", "development", "region-ph06/speedtests/test.xlsx")]
    [InlineData("http://127.0.0.1:10000/dev_storage_node/development/region-ph06/speedtests/test.xlsx", "development", "region-ph06/speedtests/test.xlsx")]
    [InlineData("http://azurite:10000/dev_storage_node/development/region-ph06/speedtests/test.xlsx", "development", "region-ph06/speedtests/test.xlsx")]
    [InlineData("https://myaccount.blob.core.windows.net/production/region-01/cells/test.xlsx", "production", "region-01/cells/test.xlsx")]
    [InlineData("https://myaccount.blob.core.windows.net/development/region-01/speedtests/Speedtest%20(1).xlsx", "development", "region-01/speedtests/Speedtest (1).xlsx")]
    public void ExtractBlobInfo_ParsesValidUrlsCorrectly(string url, string expectedContainer, string expectedBlob)
    {
        var uri = new Uri(url);
        var (container, blob) = ProcessingJobHelper.ExtractBlobInfo(uri);

        Assert.Equal(expectedContainer, container);
        Assert.Equal(expectedBlob, blob);
    }

    [Theory]
    [InlineData("http://localhost:10000/")]
    [InlineData("http://localhost:10000/onlyone")]
    public void ExtractBlobInfo_ThrowsOnInvalidUrl(string url)
    {
        var uri = new Uri(url);
        Assert.Throws<ArgumentException>(() => ProcessingJobHelper.ExtractBlobInfo(uri));
    }
}
