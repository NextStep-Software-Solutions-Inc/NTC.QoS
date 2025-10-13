using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;
using System.Web;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessCellularCoverageJob : IProcessCellularCoverageJob
{
    private readonly ILogger<ProcessCellularCoverageJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.Network> _networkRepository;
    private readonly IRepository<Domain.Entities.CellularCoverage> _cellularCoverageRepository;
    private readonly IBlobStorageService _blobStorageService;

    public ProcessCellularCoverageJob(
        ILogger<ProcessCellularCoverageJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Network> networkRepository,
        IRepository<Domain.Entities.CellularCoverage> cellularCoverageRepository,
        IBlobStorageService blobStorageService
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _networkRepository = networkRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
        _blobStorageService = blobStorageService;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file is null)
        {
            const string message = "File not found.";
            _logger.LogError("{Message}", message);
            throw new InvalidOperationException(message);
        }

        file.Status = FileStatusEnum.Processing;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        try
        {
            var cellularCoverageList = new List<Domain.Entities.CellularCoverage>();

            _logger.LogInformation("Pre-loading reference data.");

            var networkCache = await _networkRepository
                .Query(n => !n.IsDeleted)
                .Select(n => new { n.Id, n.Name })
                .ToDictionaryAsync(
                    n => CacheKeyGenerator.GenerateSimpleNetworkCacheKey(n.Name),
                    n => n.Id,
                    token
                );
            _logger.LogInformation("Pre-loaded {Count} networks.", networkCache.Count);

            // Download file from blob storage or access local file
            Stream fileStream;
            try
            {
                if (Uri.TryCreate(parameters.Path, UriKind.Absolute, out var blobUri))
                {
                    // Extract container and blob name from the blob URL
                    var (containerName, blobName) = ExtractBlobInfo(blobUri);
                    
                    _logger.LogInformation("Checking if Cellular Coverage file exists in blob storage: container={ContainerName}, blob={BlobName}", 
                        containerName, blobName);
                    
                    // Check if the blob exists before attempting to download
                    var exists = await _blobStorageService.ExistsAsync(containerName, blobName, token);
                    if (!exists)
                    {
                        _logger.LogError("Cellular Coverage file not found in blob storage: container={ContainerName}, blob={BlobName}. " +
                            "The file may have been deleted or never uploaded successfully.", containerName, blobName);
                        
                        await MarkFileAsFailed(file, "File not found in storage", token);
                        return;
                    }
                    
                    _logger.LogInformation("Downloading Cellular Coverage file from blob storage: container={ContainerName}, blob={BlobName}", 
                        containerName, blobName);
                    
                    // Download to MemoryStream to support seeking operations
                    using var blobStream = await _blobStorageService.DownloadAsync(containerName, blobName, token);
                    var memoryStream = new MemoryStream();
                    await blobStream.CopyToAsync(memoryStream, token);
                    memoryStream.Position = 0;
                    fileStream = memoryStream;
                }
                else
                {
                    // Fallback for legacy local file paths
                    _logger.LogWarning("Using legacy file path access for Cellular Coverage file: {FilePath}", parameters.Path);
                    
                    if (!System.IO.File.Exists(parameters.Path))
                    {
                        _logger.LogError("Legacy Cellular Coverage file not found on disk: {FilePath}", parameters.Path);
                        
                        await MarkFileAsFailed(file, "File not found on disk", token);
                        return;
                    }
                    
                    fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
                }
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Cellular Coverage file not found: {FilePath}. Marking file as failed.", parameters.Path);
                await MarkFileAsFailed(file, "File not found in storage", token);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to access Cellular Coverage file: {FilePath}. This could be due to file deletion, network issues, or storage problems.", parameters.Path);
                await MarkFileAsFailed(file, $"Failed to access file: {ex.Message}", token);
                return;
            }

            using (fileStream)
            {
                _logger.LogInformation("Processing Cellular Coverage file downloaded from blob storage");
                
                fileStream.Position = 0;
                _logger.LogInformation("Excel processing started.");

                var cellularCoverageRecords = ExcelHelper.ParseExcelToClass<CellularCoverageRecord>(fileStream);

                if (cellularCoverageRecords.Count is 0)
                {
                    _logger.LogInformation("The Excel is empty.");
                    file.Status = FileStatusEnum.Success;
                    _fileRepository.Update(file);
                    await _fileRepository.SaveChangesAsync(token);
                    return;
                }

                foreach (var record in cellularCoverageRecords)
                {
                    _logger.LogInformation("Processing record: {Record}", record);

                    ReportProgress($"Processing record: {record.Network}, {record.Latitude}, {record.Longitude}, {record.Strength}, {record.Address}");

                    if (!networkCache.TryGetValue(record.Network, out var networkId))
                    {
                        var networkQuery = _networkRepository
                            .Query(n => !n.IsDeleted && n.Name.Equals(record.Network));

                        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, token);
                        if (network is null)
                        {
                            _logger.LogWarning("Network {Network} not found", record.Network);
                            await TerminateProcess(file, token);
                            return;
                        }
                        else
                        {
                            networkId = network.Id;
                            networkCache[CacheKeyGenerator.GenerateSimpleNetworkCacheKey(record.Network)] = networkId;
                            _logger.LogInformation("Network {Network} found with ID {NetworkId}", record.Network, networkId);
                        }
                    }

                    if (networkId != Guid.Empty)
                    {
                        var cellularCoverage = new Domain.Entities.CellularCoverage
                        {
                            NetworkId = networkId,
                            Location = GeometryHelper.CreatePoint(new Coordinate(x: record.Longitude, y: record.Latitude)),
                            Strength = record.Strength,
                            Address = record.Address,
                            IsFileUploaded = true,
                        };
                        cellularCoverageList.Add(cellularCoverage);
                    }
                    else
                    {
                        _logger.LogWarning("Network ID is empty for network {Network}", record.Network);
                    }
                }

                if (cellularCoverageList.Count != 0)
                {
                    _cellularCoverageRepository.AddRange(cellularCoverageList);
                    await _cellularCoverageRepository.SaveChangesAsync(token);
                    _logger.LogInformation("{Count} cellular coverage records saved successfully (batched).", cellularCoverageList.Count);
                }
                else
                {
                    _logger.LogInformation("No cellular coverage records to add.");
                }

                file.Status = FileStatusEnum.Success;
                _fileRepository.Update(file);
                await _fileRepository.SaveChangesAsync(token);

                _logger.LogInformation("File processing completed successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Cellular Coverage file.");
            await TerminateProcess(file, token);
            throw;
        }
    }

    void ReportProgress(string message)
    {
        _logger.LogInformation(message);
    }

    /// <summary>
    /// Extracts container name and blob name from a blob storage URL
    /// </summary>
    private static (string containerName, string blobName) ExtractBlobInfo(Uri blobUri)
    {
        // Azure blob URLs typically follow the pattern: https://{account}.blob.core.windows.net/{container}/{blob}
        var segments = blobUri.Segments.Skip(1).ToArray(); // Skip the first '/' segment
        
        if (segments.Length < 2)
        {
            throw new ArgumentException($"Invalid blob URL format: {blobUri}");
        }

        var containerName = segments[0].TrimEnd('/');
        var blobName = string.Join("", segments.Skip(1));

        // URL decode the blob name to handle special characters like parentheses
        blobName = HttpUtility.UrlDecode(blobName);

        return (containerName, blobName);
    }

    private async Task TerminateProcess(Domain.Entities.File file, CancellationToken cancellationToken)
    {
        file.Status = FileStatusEnum.Failed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Process terminated due to error in runtime.");
    }

    private async Task MarkFileAsFailed(Domain.Entities.File file, string reason, CancellationToken cancellationToken)
    {
        file.Status = FileStatusEnum.Failed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Cellular Coverage file marked as failed: {Reason}", reason);
    }
}
