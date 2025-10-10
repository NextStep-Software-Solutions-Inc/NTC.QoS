using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BlobStorage;

namespace NTC.QoS.Application.Features.BlobStorage.Requests;

/// <summary>
/// Request to upload a blob to Azure Blob Storage
/// </summary>
public class UploadBlobRequest : IRequest<ApiResponse<BlobUploadResponseDto>>
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
}

/// <summary>
/// Request to download a blob from Azure Blob Storage
/// </summary>
public class DownloadBlobRequest : IRequest<ApiResponse<BlobDownloadDto>>
{
    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The blob file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Request to download a blob as byte array from Azure Blob Storage
/// </summary>
public class DownloadBlobAsBytesRequest : IRequest<ApiResponse<byte[]>>
{
    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The blob file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Request to delete a blob from Azure Blob Storage
/// </summary>
public class DeleteBlobRequest : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The blob file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Request to check if a blob exists in Azure Blob Storage
/// </summary>
public class BlobExistsRequest : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The blob file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Request to list blobs in a container
/// </summary>
public class ListBlobsRequest : IRequest<ApiResponse<IEnumerable<BlobInfoDto>>>
{
    /// <summary>
    /// The container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Optional prefix to filter blobs
    /// </summary>
    public string? Prefix { get; set; }
}