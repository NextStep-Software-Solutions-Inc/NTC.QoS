namespace NTC.QoS.Domain.Models;

/// <summary>
/// Domain model for uploaded files (framework-agnostic)
/// </summary>
public class FileUpload
{
    /// <summary>
    /// Original filename
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Content type / MIME type
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Length { get; init; }

    /// <summary>
    /// File content as stream
    /// </summary>
    public required Stream Content { get; init; }
}
