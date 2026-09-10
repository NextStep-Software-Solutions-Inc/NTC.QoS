using System;
using Azure.Storage;
using NTC.QoS.Infrastructure.Helper;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Helper;

public class ProcessingJobHelperTests
{
    private const string TestAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    [Theory]
    [InlineData("http://localhost:10000/dev_storage_node/development/region-ph06/speedtests/test.xlsx", "dev_storage_node", "development", "region-ph06/speedtests/test.xlsx")]
    [InlineData("http://127.0.0.1:10000/dev_storage_node/development/region-ph06/speedtests/test.xlsx", "dev_storage_node", "development", "region-ph06/speedtests/test.xlsx")]
    [InlineData("http://azurite:10000/dev_storage_node/development/region-ph06/speedtests/test.xlsx", "dev_storage_node", "development", "region-ph06/speedtests/test.xlsx")]
    [InlineData("https://blob.qos.ntc.gov.ph/devstoreaccount1/production/region-ph06/speedtests/test.xlsx", "devstoreaccount1", "production", "region-ph06/speedtests/test.xlsx")]
    [InlineData("https://blob.qos.ntc.gov.ph/qosstorage/production/region-ph06/speedtests/test.xlsx", "qosstorage", "production", "region-ph06/speedtests/test.xlsx")]
    [InlineData("https://myaccount.blob.core.windows.net/production/region-01/cells/test.xlsx", null, "production", "region-01/cells/test.xlsx")]
    [InlineData("https://myaccount.blob.core.windows.net/development/region-01/speedtests/Speedtest%20(1).xlsx", null, "development", "region-01/speedtests/Speedtest (1).xlsx")]
    public void ExtractBlobInfo_ParsesValidUrlsCorrectly(string url, string? storageAccountName, string expectedContainer, string expectedBlob)
    {
        var uri = new Uri(url);
        var (container, blob) = ProcessingJobHelper.ExtractBlobInfo(uri, storageAccountName);

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

    [Theory]
    [InlineData("DefaultEndpointsProtocol=https;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=https://blob.qos.ntc.gov.ph/devstoreaccount1;",
        "https://blob.qos.ntc.gov.ph/devstoreaccount1/production/region-ph06/speedtests/file.xlsx")]
    [InlineData("DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
        "http://127.0.0.1:10000/devstoreaccount1/production/region-ph06/speedtests/file.xlsx")]
    [InlineData("DefaultEndpointsProtocol=https;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=https://blob.qos.ntc.gov.ph;",
        "https://blob.qos.ntc.gov.ph/production/region-ph06/speedtests/file.xlsx")]
    [InlineData("DefaultEndpointsProtocol=https;AccountName=mycloudaccount;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;EndpointSuffix=core.windows.net;",
        "https://mycloudaccount.blob.core.windows.net/production/region-ph06/speedtests/file.xlsx")]
    public void BlobStorageService_GetBlobUrl_And_PreSignedUrl_SupportAllEndpointStyles(string connStr, string expectedBlobUri)
    {
        var client = new Azure.Storage.Blobs.BlobServiceClient(connStr);
        var options = Microsoft.Extensions.Options.Options.Create(new NTC.QoS.Infrastructure.Configuration.BlobStorageOptions());
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<NTC.QoS.Infrastructure.Services.BlobStorageService>.Instance;
        var credential = new StorageSharedKeyCredential(client.AccountName, TestAccountKey);
        var service = new NTC.QoS.Infrastructure.Services.BlobStorageService(client, options, logger, credential);

        var url = service.GetBlobUrl("production", "region-ph06/speedtests/file.xlsx");
        Assert.Equal(expectedBlobUri, url);

        var sasUploadUrl = service.GeneratePreSignedUploadUrl("production", "region-ph06/speedtests/file.xlsx");
        Assert.StartsWith(expectedBlobUri, sasUploadUrl);

        var sasDownloadUrl = service.GeneratePreSignedDownloadUrl("production", "region-ph06/speedtests/file.xlsx");
        Assert.StartsWith(expectedBlobUri, sasDownloadUrl);
    }
}
