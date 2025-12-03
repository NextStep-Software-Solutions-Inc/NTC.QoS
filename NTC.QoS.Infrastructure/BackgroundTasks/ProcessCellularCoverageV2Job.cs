using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.CsvBindings;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Infrastructure.FileProcessing;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job for processing CellularCoverage V2 CSV uploads
/// Derives BandId and TechnologyId from Network
/// </summary>
public class ProcessCellularCoverageV2Job : IProcessCellularCoverageV2Job
{
    private readonly ILogger<ProcessCellularCoverageV2Job> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly ICellularCoverageV2Repository _coverageV2Repository;
    private readonly ICellularCoverageV2MappingService _mappingService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IRepository<Network> _networkRepository;

    public ProcessCellularCoverageV2Job(
        ILogger<ProcessCellularCoverageV2Job> logger,
        IRepository<Domain.Entities.File> fileRepository,
        ICellularCoverageV2Repository coverageV2Repository,
        ICellularCoverageV2MappingService mappingService,
        IBlobStorageService blobStorageService,
        IRepository<Network> networkRepository)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _coverageV2Repository = coverageV2Repository;
        _mappingService = mappingService;
        _blobStorageService = blobStorageService;
        _networkRepository = networkRepository;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(
        FileDto fileDto,
        Guid networkId,
        Guid serviceProviderId,
        Guid municipalityId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting CellularCoverage V2 processing for FileId={FileId}, NetworkId={NetworkId}, ServiceProviderId={ServiceProviderId}, MunicipalityId={MunicipalityId}",
            fileDto.Id, networkId, serviceProviderId, municipalityId);

        // Step 1: Fetch and validate Network (to derive BandId and TechnologyId)
        var network = await _networkRepository
            .Query()
            .Include(n => n.SystemType)
            .ThenInclude(st => st.Technology)
            .FirstOrDefaultAsync(n => n.Id == networkId, cancellationToken);

        if (network == null)
        {
            _logger.LogError("Network not found with Id={NetworkId}", networkId);
            throw new InvalidOperationException($"Network with Id {networkId} not found");
        }

        if (network.SystemType == null || network.SystemType.Technology == null)
        {
            _logger.LogError("Network {NetworkId} has no SystemType or Technology configured", networkId);
            throw new InvalidOperationException($"Network {networkId} is missing SystemType or Technology configuration");
        }

        var bandId = network.BandId;
        var technologyId = network.SystemType.TechnologyId;

            _logger.LogInformation(
                "Derived from Network: BandId={BandId}, TechnologyId={TechnologyId}",
                bandId, technologyId);

        // Step 2: Validate and retrieve file record
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(
            fileDto.Id,
            _fileRepository,
            _logger,
            FileTypeEnum.CellularCoverageV2,
            cancellationToken);

        // Step 3: Update file status to Processing
        await ProcessingJobHelper.SetFileProcessingStatusAsync(
            file,
            _fileRepository,
            _logger,
            FileTypeEnum.CellularCoverageV2,
            cancellationToken);

        // Processing statistics
        var stats = new ProcessingStats();        try
        {
            // Step 4: Download file from blob storage
            Stream fileStream;
            try
            {
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(
                    fileDto.Path,
                    _blobStorageService,
                    _logger,
                    FileTypeEnum.CellularCoverageV2,
                    cancellationToken);
            }
            catch (FileNotFoundException ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex,
                    fileDto.Path,
                    file,
                    _fileRepository,
                    _logger,
                    FileTypeEnum.CellularCoverageV2,
                    cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex,
                    fileDto.Path,
                    file,
                    _fileRepository,
                    _logger,
                    FileTypeEnum.CellularCoverageV2,
                    cancellationToken);
                return;
            }

            using (fileStream)
            {
                _logger.LogInformation("Processing CSV file using STREAMING mode (memory-efficient for large files)");

                // Step 5: Initialize mapping service with cached data (CRITICAL for performance)
                _logger.LogInformation("Pre-loading CellSectors and network data for efficient processing...");
                await _mappingService.InitializeAsync(networkId, municipalityId, cancellationToken);

                // Step 6: Stream CSV in batches to avoid loading entire file into memory
                const int csvBatchSize = 1000;   // Read 1000 CSV records at a time from stream
                const int insertBatchSize = 500; // Insert 500 to DB at a time

                var allMappedRecords = new List<CellularCoverageVersion2>();
                int totalRecordsProcessed = 0;
                int batchNumber = 0;

                try
                {
                    // Stream CSV batches without loading entire file into memory
                    foreach (var csvBatch in CsvParser.StreamCellularCoverageV2CsvBatches(fileStream, csvBatchSize, _logger))
                    {
                        batchNumber++;
                        totalRecordsProcessed += csvBatch.Count;
                        stats.TotalRows += csvBatch.Count;

                        _logger.LogInformation(
                            "Processing CSV batch {BatchNumber} ({Count} records, total: {Total})",
                            batchNumber, csvBatch.Count, totalRecordsProcessed);

                        // Map entire batch at once (minimal DB queries)
                        var mappedBatch = await _mappingService.MapCoveragePointsBatchAsync(
                            csvBatch,
                            networkId,
                            bandId,
                            municipalityId,
                            cancellationToken);

                        stats.ProcessedSuccessfully += mappedBatch.Count;
                        stats.Skipped += csvBatch.Count - mappedBatch.Count;

                        allMappedRecords.AddRange(mappedBatch);

                        // Insert to database when we have enough records
                        if (allMappedRecords.Count >= insertBatchSize)
                        {
                            await InsertBatchAsync(allMappedRecords, cancellationToken);
                            _logger.LogInformation("Inserted {Count} records to database (total processed: {Total})",
                                allMappedRecords.Count, stats.ProcessedSuccessfully);
                            allMappedRecords.Clear();
                        }
                    }

                    // Insert remaining records
                    if (allMappedRecords.Count > 0)
                    {
                        await InsertBatchAsync(allMappedRecords, cancellationToken);
                        _logger.LogInformation("Inserted final batch of {Count} records", allMappedRecords.Count);
                    }

                    if (stats.TotalRows == 0)
                    {
                        _logger.LogInformation("CSV file is empty");
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Streaming processing completed. Total={Total}, Processed={Processed}, Skipped={Skipped}",
                            stats.TotalRows, stats.ProcessedSuccessfully, stats.Skipped);
                    }

                    await ProcessingJobHelper.SetFileSuccessStatusAsync(
                        file,
                        _fileRepository,
                        _logger,
                        FileTypeEnum.CellularCoverageV2,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during streaming CSV processing");
                    await ProcessingJobHelper.TerminateProcessAsync(
                        file,
                        _fileRepository,
                        _logger,
                        FileTypeEnum.CellularCoverageV2,
                        $"Streaming processing error: {ex.Message}",
                        cancellationToken);
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing CellularCoverage V2 file");
            await ProcessingJobHelper.TerminateProcessAsync(
                file,
                _fileRepository,
                _logger,
                FileTypeEnum.CellularCoverageV2,
                $"Processing error: {ex.Message}",
                cancellationToken);
            throw;
        }
    }

    private async Task InsertBatchAsync(
        List<CellularCoverageVersion2> records,
        CancellationToken cancellationToken)
    {
        try
        {
            await _coverageV2Repository.BulkInsertAsync(records, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk insert of {Count} records", records.Count);
            throw;
        }
    }

    private class ProcessingStats
    {
        public int TotalRows { get; set; }
        public int ProcessedSuccessfully { get; set; }
        public int Skipped { get; set; }
    }
}
