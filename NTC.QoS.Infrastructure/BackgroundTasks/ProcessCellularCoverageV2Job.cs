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
/// Derives ServiceProviderId and TechnologyId from Network
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
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting CellularCoverage V2 processing for FileId={FileId}, NetworkId={NetworkId}, BandId={BandId}, MunicipalityId={MunicipalityId}",
            fileDto.Id, networkId, bandId, municipalityId);

        // Step 1: Fetch and validate Network (to derive ServiceProviderId and TechnologyId)
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

        var serviceProviderId = network.ServiceProviderId;
        var technologyId = network.SystemType.TechnologyId;

        _logger.LogInformation(
            "Derived from Network: ServiceProviderId={ServiceProviderId}, TechnologyId={TechnologyId}",
            serviceProviderId, technologyId);

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
        var stats = new ProcessingStats();

        try
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
                _logger.LogInformation("Processing CSV file from blob storage");

                // Step 5: Validate CSV headers
                if (!CsvParser.ValidateCsvHeaders(fileStream, _logger))
                {
                    await ProcessingJobHelper.TerminateProcessAsync(
                        file,
                        _fileRepository,
                        _logger,
                        FileTypeEnum.CellularCoverageV2,
                        "CSV file must contain columns: Lat, Lng, Signal, Address",
                        cancellationToken);
                    return;
                }

                // Step 6: Parse CSV records
                List<CellularCoverageV2CsvRecord> csvRecords;
                try
                {
                    csvRecords = CsvParser.ParseCellularCoverageV2Csv(fileStream, _logger);
                    stats.TotalRows = csvRecords.Count;
                    _logger.LogInformation("Parsed {Count} records from CSV", csvRecords.Count);
                }
                catch (Exception ex)
                {
                    await ProcessingJobHelper.TerminateProcessAsync(
                        file,
                        _fileRepository,
                        _logger,
                        FileTypeEnum.CellularCoverageV2,
                        $"CSV parsing failed: {ex.Message}",
                        cancellationToken);
                    return;
                }

                if (csvRecords.Count == 0)
                {
                    _logger.LogInformation("CSV file is empty");
                    await ProcessingJobHelper.SetFileSuccessStatusAsync(
                        file,
                        _fileRepository,
                        _logger,
                        FileTypeEnum.CellularCoverageV2,
                        cancellationToken);
                    return;
                }

                // Step 7: Process records in batches
                var mappedRecords = new List<CellularCoverageVersion2>();
                const int batchSize = 100;

                for (int i = 0; i < csvRecords.Count; i++)
                {
                    var record = csvRecords[i];

                    ProcessingJobHelper.ReportProgress(
                        $"Processing record {i + 1}/{csvRecords.Count}: Lat={record.Latitude}, Lng={record.Longitude}",
                        _logger);

                    // Map CSV record to V2 entity
                    var mappedEntity = await _mappingService.MapCoveragePointAsync(
                        record,
                        networkId,
                        bandId,
                        municipalityId,
                        cancellationToken);

                    if (mappedEntity != null)
                    {
                        mappedRecords.Add(mappedEntity);
                        stats.ProcessedSuccessfully++;
                    }
                    else
                    {
                        stats.Skipped++;
                        _logger.LogDebug("Skipped record at row {Row}", i + 1);
                    }

                    // Bulk insert when batch is full
                    if (mappedRecords.Count >= batchSize)
                    {
                        await InsertBatchAsync(mappedRecords, cancellationToken);
                        _logger.LogInformation("Inserted batch of {Count} records", mappedRecords.Count);
                        mappedRecords.Clear();
                    }
                }

                // Insert remaining records
                if (mappedRecords.Count > 0)
                {
                    await InsertBatchAsync(mappedRecords, cancellationToken);
                    _logger.LogInformation("Inserted final batch of {Count} records", mappedRecords.Count);
                }

                // Step 7: Update file status to Success
                _logger.LogInformation(
                    "Processing completed. Total={Total}, Processed={Processed}, Skipped={Skipped}",
                    stats.TotalRows, stats.ProcessedSuccessfully, stats.Skipped);

                await ProcessingJobHelper.SetFileSuccessStatusAsync(
                    file,
                    _fileRepository,
                    _logger,
                    FileTypeEnum.CellularCoverageV2,
                    cancellationToken);
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
