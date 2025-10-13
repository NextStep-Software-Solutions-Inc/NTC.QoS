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
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(
            parameters.Id, _fileRepository, _logger, "Cellular Coverage", token);

        await ProcessingJobHelper.SetFileProcessingStatusAsync(
            file, _fileRepository, _logger, "Cellular Coverage", token);

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
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(
                    parameters.Path, _blobStorageService, _logger, "Cellular Coverage", token);
            }
            catch (FileNotFoundException ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, "Cellular Coverage", token);
                return;
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, "Cellular Coverage", token);
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
                    await ProcessingJobHelper.SetFileSuccessStatusAsync(
                        file, _fileRepository, _logger, "Cellular Coverage", token);
                    return;
                }

                foreach (var record in cellularCoverageRecords)
                {
                    _logger.LogInformation("Processing record: {Record}", record);

                    ProcessingJobHelper.ReportProgress($"Processing record: {record.Network}, {record.Latitude}, {record.Longitude}, {record.Strength}, {record.Address}", _logger);

                    if (!networkCache.TryGetValue(record.Network, out var networkId))
                    {
                        var networkQuery = _networkRepository
                            .Query(n => !n.IsDeleted && n.Name.Equals(record.Network));

                        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, token);
                        if (network is null)
                        {
                            _logger.LogWarning("Network {Network} not found", record.Network);
                            await ProcessingJobHelper.TerminateProcessAsync(
                                file, _fileRepository, _logger, "Cellular Coverage", 
                                $"Network '{record.Network}' not found in database", token);
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

                await ProcessingJobHelper.SetFileSuccessStatusAsync(
                    file, _fileRepository, _logger, "Cellular Coverage", token);

                _logger.LogInformation("File processing completed successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Cellular Coverage file.");
            await ProcessingJobHelper.TerminateProcessAsync(
                file, _fileRepository, _logger, "Cellular Coverage", 
                $"Error occurred while processing: {ex.Message}", token);
            throw;
        }
    }
}
