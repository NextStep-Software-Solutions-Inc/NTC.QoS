using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for Azure Blob Storage operations with region support
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The name to give the blob</param>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="contentType">The content type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URI of the uploaded blob</returns>
    Task<string> UploadAsync(string containerName, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The blob name to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A stream containing the blob content</returns>
    Task<Stream> DownloadAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from Azure Blob Storage as byte array
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The blob name to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blob content as byte array</returns>
    Task<byte[]> DownloadAsBytesAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The blob name to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the blob was deleted, false if it didn't exist</returns>
    Task<bool> DeleteAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists in Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The blob name to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the blob exists, false otherwise</returns>
    Task<bool> ExistsAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the URL of a blob
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The blob name</param>
    /// <returns>The blob URL</returns>
    string GetBlobUrl(string containerName, string fileName);

    /// <summary>
    /// Lists all blobs in a container with optional prefix filter
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="prefix">Optional prefix to filter blobs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of blob names</returns>
    Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blobs by region and file type
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="region">Region code (e.g., "region10")</param>
    /// <param name="fileType">Optional file type filter (e.g., "cells")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of blob names</returns>
    Task<IEnumerable<string>> ListBlobsByRegionAsync(string containerName, string region, string? fileType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed SAS URL for uploading a blob (valid for specified duration, default 1 min)
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The blob path/name</param>
    /// <param name="expiryWindow">Optional expiry window (defaults to 1 minute)</param>
    /// <returns>Pre-signed upload SAS URL</returns>
    string GeneratePreSignedUploadUrl(string containerName, string fileName, TimeSpan? expiryWindow = null);

    /// <summary>
    /// Generates a pre-signed SAS URL for downloading a blob (valid for specified duration, default 1 min)
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="fileName">The blob path/name</param>
    /// <param name="expiryWindow">Optional expiry window (defaults to 1 minute)</param>
    /// <returns>Pre-signed download SAS URL</returns>
    string GeneratePreSignedDownloadUrl(string containerName, string fileName, TimeSpan? expiryWindow = null);
}