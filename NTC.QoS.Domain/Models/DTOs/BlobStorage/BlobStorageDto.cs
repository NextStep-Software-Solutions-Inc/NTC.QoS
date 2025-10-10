using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.BlobStorage;

/// <summary>
/// DTO for blob upload request
/// </summary>
public class BlobUploadDto
{
    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The blob file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The file stream
    /// </summary>
    public Stream FileStream { get; set; } = Stream.Null;

    /// <summary>
    /// The content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// DTO for blob upload response
/// </summary>
public class BlobUploadResponseDto
{
    /// <summary>
    /// The blob URL
    /// </summary>
    public string BlobUrl { get; set; } = string.Empty;

    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The blob name
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for blob download response
/// </summary>
public class BlobDownloadDto
{
    /// <summary>
    /// The file stream
    /// </summary>
    public Stream FileStream { get; set; } = Stream.Null;

    /// <summary>
    /// The content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The file size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime LastModified { get; set; }
}

/// <summary>
/// DTO for blob information
/// </summary>
public class BlobInfoDto
{
    /// <summary>
    /// The blob name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The blob URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The file size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Blob metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// DTO for blob upload form request
/// </summary>
public class BlobUploadFormDto
{
    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The file to upload
    /// </summary>
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Optional custom file name
    /// </summary>
    public string? FileName { get; set; }
}