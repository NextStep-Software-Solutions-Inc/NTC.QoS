namespace NTC.QoS.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Azure Blob Storage
/// </summary>
public class BlobStorageOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "AzureBlobStorage";


    /// <summary>
    /// Maximum file size in MB
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 2048;

    /// <summary>
    /// Allowed content types for uploads
    /// </summary>
    public string[] AllowedContentTypes { get; set; } =
    [
        "application/pdf",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/csv",
        "image/jpeg",
        "image/png",
        "image/gif",
        "text/plain"
    ];

    /// <summary>
    /// Whether to use development storage emulator
    /// </summary>
    public bool UseDevelopmentStorage { get; set; } = false;

    /// <summary>
    /// Container access level (Private, Blob, Container)
    /// </summary>
    public string AccessLevel { get; set; } = "Private";
}