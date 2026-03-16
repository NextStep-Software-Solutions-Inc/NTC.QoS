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
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Domain.Entities.CellularCoverage> _cellularCoverageRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICellularCoverageMappingService _mappingService;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IGenerateConnectivityAssessmentJob _generateConnectivityAssessmentJob;
    private readonly IUserContextService _userContextService;

    public ProcessCellularCoverageJob(
        ILogger<ProcessCellularCoverageJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Network> networkRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.CellularCoverage> cellularCoverageRepository,
        IBlobStorageService blobStorageService,
        ICellularCoverageMappingService mappingService,
        IMunicipalityRepository municipalityRepository,
        IBackgroundJobService backgroundJobService,
        IGenerateConnectivityAssessmentJob generateConnectivityAssessmentJob,
        IUserContextService userContextService
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _networkRepository = networkRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
        _blobStorageService = blobStorageService;
        _mappingService = mappingService;
        _municipalityRepository = municipalityRepository;
        _backgroundJobService = backgroundJobService;
        _generateConnectivityAssessmentJob = generateConnectivityAssessmentJob;
        _userContextService = userContextService;
    }
    [Queue("import")]
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(
            parameters.Id, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);

        // Check if file is already successfully processed
        if (file.Status == FileStatusEnum.Success)
        {
            _logger.LogInformation(
                "File {FileId} is already successfully processed. Skipping duplicate execution.",
                file.Id);
            return;
        }

        // Check if file is already being processed (duplicate job prevention)
        if (file.Status == FileStatusEnum.Processing && !string.IsNullOrEmpty(file.ProcessingJobId))
        {
            _logger.LogWarning(
                "File {FileId} is already being processed by job {ExistingJobId}. Skipping duplicate execution.",
                file.Id, file.ProcessingJobId);
            return;
        }

        await ProcessingJobHelper.SetFileProcessingStatusAsync(
            file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);

        // Resume from checkpoint if available
        int startRecordIndex = file.LastProcessedRowNumber ?? 0;

        if (startRecordIndex > 0)
        {
            _logger.LogInformation(
                "Resuming from checkpoint: Record {RecordNumber}, Last checkpoint: {CheckpointTime}",
                startRecordIndex, file.LastCheckpointTime);
        }

        try
        {
            _logger.LogInformation("Pre-loading reference data.");

            // Initialize mapping service (pre-load CellSectors and Municipalities)
            await _mappingService.InitializeAsync(token);

            // Pre-load WIRELESS service providers only (handle duplicates by grouping)
            var serviceProviders = await _serviceProviderRepository
                .Query(sp => !sp.IsDeleted)
                .Include(sp => sp.Service)
                .Where(sp => sp.Service.Type == ServiceTypeEnum.Wireless)
                .Select(sp => new { sp.Id, sp.Provider.Name })
                .ToListAsync(token);

            var serviceProviderCache = new Dictionary<string, Guid>();
            var serviceProviderIdToName = new Dictionary<Guid, string>();
            
            foreach (var group in serviceProviders.GroupBy(sp => CacheKeyGenerator.GenerateSimpleNetworkCacheKey(sp.Name)))
            {
                if (group.Count() > 1)
                {
                    var duplicateIds = string.Join(", ", group.Select(sp => sp.Id));
                    _logger.LogWarning(
                        "Duplicate wireless ServiceProviders found with normalized name '{NormalizedName}': {Count} providers ({Ids}). Using first: {SelectedId}",
                        group.Key, group.Count(), duplicateIds, group.First().Id);
                }
                var firstProvider = group.First();
                serviceProviderCache[group.Key] = firstProvider.Id;
                serviceProviderIdToName[firstProvider.Id] = firstProvider.Name;
            }
            _logger.LogInformation("Pre-loaded {Count} wireless service providers.", serviceProviderCache.Count);

            // Pre-load networks with Band and Technology metadata
            // NOTE: Networks are cached by name only (not by ServiceProvider) because
            // we will filter by ServiceProvider during lookup to ensure correct network selection
            var networks = await _networkRepository
                .Query(n => !n.IsDeleted)
                .Include(n => n.SystemType)
                .ThenInclude(st => st.Technology)
                .Select(n => new { 
                    n.Id, 
                    n.Name, 
                    BandId = (Guid?)n.BandId,
                    TechnologyId = n.SystemType != null ? n.SystemType.TechnologyId : (Guid?)null,
                    ServiceProviderId = n.ServiceProviderId
                })
                .ToListAsync(token);

            _logger.LogInformation("Pre-loaded {Count} networks.", networks.Count);

            // Download file from blob storage or access local file
            Stream fileStream;
            try
            {
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(
                    parameters.Path, _blobStorageService, _logger, FileTypeEnum.CellularCoverage, token);
            }
            catch (FileNotFoundException ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);
                return;
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);
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
                        file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);
                    return;
                }

                _logger.LogInformation("Processing {Count} records from Excel file", cellularCoverageRecords.Count);

                // Pre-load existing coverage points for duplicate detection
                // Only load coverage for networks that appear in the file to minimize memory usage
                var networkIds = networks.Select(n => n.Id).ToHashSet();
                var existingCoverage = await _cellularCoverageRepository
                    .Query(cc => !cc.IsDeleted && networkIds.Contains(cc.NetworkId))
                    .Select(cc => new
                    {
                        cc.NetworkId,
                        cc.BandId,
                        cc.Location
                    })
                    .ToListAsync(token);

                _logger.LogInformation("Pre-loaded {Count} existing coverage points for duplicate detection.", existingCoverage.Count);

                // Build spatial index: group by NetworkId + BandId for faster lookups
                var spatialIndex = existingCoverage
                    .GroupBy(cc => new { cc.NetworkId, BandId = cc.BandId ?? Guid.Empty })
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(cc => cc.Location).ToList()
                    );

                _logger.LogInformation("Built spatial index with {Count} network-band combinations.", spatialIndex.Count);

                // Track statistics
                int totalRecordsProcessed = 0;
                int processedSuccessfully = 0;
                int skippedDuplicates = 0;

                // Batch processing configuration
                const int insertBatchSize = 500; // Insert 500 records to DB at a time
                var currentBatch = new List<Domain.Entities.CellularCoverage>();

                // Skip already processed records on resume
                var recordsToProcess = startRecordIndex > 0 
                    ? cellularCoverageRecords.Skip(startRecordIndex).ToList()
                    : cellularCoverageRecords;

                if (startRecordIndex > 0)
                {
                    _logger.LogInformation(
                        "Skipping {Count} already processed records, starting from record {Index}",
                        startRecordIndex, startRecordIndex + 1);
                }

                foreach (var record in recordsToProcess)
                {
                    _logger.LogInformation("Processing record: {Record}", record);

                    ProcessingJobHelper.ReportProgress($"Processing record: SP={record.ServiceProvider}, Network={record.Network}, Lat={record.Latitude}, Lng={record.Longitude}, SignalStrength={record.SignalStrength}", _logger);

                    // Validate Service Provider
                    Guid? serviceProviderId = null;
                    if (!string.IsNullOrEmpty(record.ServiceProvider))
                    {
                        if (!serviceProviderCache.TryGetValue(
                            CacheKeyGenerator.GenerateSimpleNetworkCacheKey(record.ServiceProvider), 
                            out var spId))
                        {
                            _logger.LogWarning("Service Provider '{ServiceProvider}' not found", record.ServiceProvider);
                            await ProcessingJobHelper.TerminateProcessAsync(
                                file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                                $"Service Provider '{record.ServiceProvider}' not found in database", token);
                            return;
                        }
                        serviceProviderId = spId;
                    }

                    // Find network by name and ServiceProvider from pre-loaded list
                    var networkKey = CacheKeyGenerator.GenerateSimpleNetworkCacheKey(record.Network);
                    
                    // Filter networks: match by name first
                    var matchingNetworksByName = networks
                        .Where(n => CacheKeyGenerator.GenerateSimpleNetworkCacheKey(n.Name) == networkKey)
                        .ToList();

                    _logger.LogDebug(
                        "Network lookup: Name='{Network}' (normalized: '{NormalizedKey}'), Found {Count} networks with this name",
                        record.Network, networkKey, matchingNetworksByName.Count);

                    // If ServiceProvider is provided, filter by ServiceProvider
                    var matchingNetworks = matchingNetworksByName;
                    if (serviceProviderId.HasValue)
                    {
                        matchingNetworks = matchingNetworksByName
                            .Where(n => n.ServiceProviderId == serviceProviderId.Value)
                            .ToList();

                        _logger.LogDebug(
                            "After ServiceProvider filter (ServiceProviderId={ServiceProviderId}): {Count} networks match",
                            serviceProviderId.Value, matchingNetworks.Count);

                        if (matchingNetworks.Count == 0 && matchingNetworksByName.Count > 0)
                        {
                            // Network exists but not for this ServiceProvider - show available providers and terminate
                            var availableProviderIds = matchingNetworksByName
                                .Select(n => n.ServiceProviderId)
                                .Distinct()
                                .ToList();
                            
                            // Resolve provider names for better error messages
                            var availableProviderNames = availableProviderIds
                                .Where(id => serviceProviderIdToName.ContainsKey(id))
                                .Select(id => serviceProviderIdToName[id])
                                .Take(5)
                                .ToList();
                            
                            var errorMsg = $"Network '{record.Network}' exists but not for Service Provider '{record.ServiceProvider}'. " +  
                                $"Available providers for this network: {string.Join(", ", availableProviderNames)}" +  
                                (availableProviderIds.Count > 5 ? $" and {availableProviderIds.Count - 5} others" : "");  
                            
                            _logger.LogError(errorMsg);
                            await ProcessingJobHelper.TerminateProcessAsync(
                                file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                                errorMsg, token);
                            return;
                        }
                    }

                    if (matchingNetworks.Count == 0)
                    {
                        var errorMsg = $"Network '{record.Network}' not found in database";
                        
                        _logger.LogError(errorMsg);
                        await ProcessingJobHelper.TerminateProcessAsync(
                            file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                            errorMsg, token);
                        return;
                    }

                    if (matchingNetworks.Count > 1)
                    {
                        var errorMsg = $"Multiple networks found with name '{record.Network}' for ServiceProvider '{record.ServiceProvider ?? "N/A"}'. " +
                            "Please specify additional fields (e.g., Band or Technology) to disambiguate, or ensure network names are unique for the ServiceProvider.";
                        _logger.LogError(errorMsg);
                        await ProcessingJobHelper.TerminateProcessAsync(
                            file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                            errorMsg, token);
                        return;
                    }

                    var networkMetadata = matchingNetworks[0];
                    
                    _logger.LogDebug(
                        "Network matched: '{Network}' -> NetworkId={NetworkId}, ServiceProviderId={ServiceProviderId}", 
                        record.Network, networkMetadata.Id, networkMetadata.ServiceProviderId);

                    // Determine final ServiceProviderId: use user-provided if available, otherwise use Network's
                    var finalServiceProviderId = serviceProviderId ?? 
                        (networkMetadata.ServiceProviderId != Guid.Empty ? networkMetadata.ServiceProviderId : null);

                    // Map to infrastructure (CellSector and Municipality) using mapping service
                    var (cellSectorId, municipalityId) = await _mappingService.FindInfrastructureMappingAsync(
                        record.Latitude, record.Longitude);

                    if (cellSectorId == null)
                    {
                        _logger.LogDebug("No CellSector found for point ({Lat}, {Lng}). Record will be saved without infrastructure mapping.",
                            record.Latitude, record.Longitude);
                    }

                    // Check for duplicate: same NetworkId, BandId, and location within 1 meter
                    // Use pre-loaded spatial index for in-memory duplicate detection
                    var pointToCheck = GeometryHelper.CreatePoint(new Coordinate(record.Longitude, record.Latitude));
                    const double spatialToleranceMeters = 1.0;
                    
                    var indexKey = new { NetworkId = networkMetadata.Id, BandId = networkMetadata.BandId ?? Guid.Empty };
                    var isDuplicate = false;
                    
                    if (spatialIndex.TryGetValue(indexKey, out var existingLocations))
                    {
                        isDuplicate = existingLocations.Any(loc => loc.Distance(pointToCheck) <= spatialToleranceMeters);
                    }

                    if (isDuplicate)
                    {
                        _logger.LogDebug("Skipping duplicate coverage point: Network={Network}, Lat={Lat}, Lng={Lng}", 
                            record.Network, record.Latitude, record.Longitude);
                        skippedDuplicates++;
                        continue;
                    }

                    // Get current user and timestamp for audit fields
                    var now = DateTime.UtcNow;
                    var userId = _userContextService.GetCurrentUserId() ?? "System";

                    // Create coverage record with all derived fields
                    var cellularCoverage = new Domain.Entities.CellularCoverage
                    {
                        NetworkId = networkMetadata.Id,
                        ServiceProviderId = finalServiceProviderId,
                        BandId = networkMetadata.BandId,
                        TechnologyId = networkMetadata.TechnologyId,
                        CellSectorId = cellSectorId,
                        MunicipalityId = municipalityId,
                        Location = GeometryHelper.CreatePoint(new Coordinate(x: record.Longitude, y: record.Latitude)),
                        SignalStrength = record.SignalStrength,
                        Address = record.Address,
                        IsFileUploaded = true,
                        // CRITICAL: Manually set audit properties since BulkInsertAsync bypasses interceptors
                        CreatedAt = now,
                        CreatedBy = userId,
                        UpdatedAt = now,
                        UpdatedBy = userId
                    };
                    currentBatch.Add(cellularCoverage);
                    processedSuccessfully++;
                    totalRecordsProcessed++;
                    
                    // Update spatial index with new point to catch duplicates within the same file
                    if (!spatialIndex.ContainsKey(indexKey))
                    {
                        spatialIndex[indexKey] = new List<Point>();
                    }
                    spatialIndex[indexKey].Add(cellularCoverage.Location);

                    // Insert batch when it reaches the batch size
                    if (currentBatch.Count >= insertBatchSize)
                    {
                        await InsertBatchAsync(currentBatch, token);
                        _logger.LogInformation(
                            "Inserted batch of {Count} records (total processed: {Total}/{TotalRecords})",
                            currentBatch.Count, totalRecordsProcessed + startRecordIndex, cellularCoverageRecords.Count);
                        currentBatch.Clear();
                        
                        // Save checkpoint after successful database insert
                        await SaveCheckpointAsync(file, startRecordIndex + totalRecordsProcessed, token);
                    }
                }

                // Insert remaining records
                if (currentBatch.Count > 0)
                {
                    await InsertBatchAsync(currentBatch, token);
                    _logger.LogInformation(
                        "Inserted final batch of {Count} records (total: {Total})",
                        currentBatch.Count, processedSuccessfully);
                    
                    // Save final checkpoint
                    await SaveCheckpointAsync(file, startRecordIndex + totalRecordsProcessed, token);
                }

                if (processedSuccessfully == 0 && skippedDuplicates == 0)
                {
                    _logger.LogInformation("No new cellular coverage records to add (Excel file was empty or all already processed).");
                }
                else
                {
                    _logger.LogInformation(
                        "Processing complete: Total={Total}, Processed={Processed}, Duplicates={Duplicates}",
                        cellularCoverageRecords.Count, processedSuccessfully, skippedDuplicates);
                }

                // Clear checkpoint on success
                await ClearCheckpointAsync(file, token);

                await ProcessingJobHelper.SetFileSuccessStatusAsync(
                    file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);

                // Trigger connectivity assessment generation upon successful processing
                _logger.LogInformation("Cellular coverage processing completed successfully. Triggering connectivity assessment generation for file {FileId}", file.Id);
                _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(job => job.ExecuteAsync(file.Id, CancellationToken.None));

                _logger.LogInformation("File processing completed successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Cellular Coverage file.");
            await ProcessingJobHelper.TerminateProcessAsync(
                file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                $"Error occurred while processing: {ex.Message}", token);
            throw;
        }
    }

    /// <summary>
    /// Inserts a batch of records using bulk insert for better performance
    /// </summary>
    private async Task InsertBatchAsync(
        List<Domain.Entities.CellularCoverage> records,
        CancellationToken cancellationToken)
    {
        try
        {
            await _cellularCoverageRepository.BulkInsertAsync(records, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("FK_CellularCoverages_Municipalities_MunicipalityId") == true)
        {
            // Log the problematic municipality IDs for debugging
            var uniqueMunicipalityIds = records
                .Where(c => c.MunicipalityId.HasValue)
                .Select(c => c.MunicipalityId!.Value)
                .Distinct()
                .ToList();

            _logger.LogError(ex, 
                "Foreign key constraint violation for MunicipalityId. Attempted to insert records with {Count} unique municipality IDs: {Ids}",
                uniqueMunicipalityIds.Count, string.Join(", ", uniqueMunicipalityIds));

            // Verify which IDs don't exist in the database
            var existingIds = await _municipalityRepository
                .Query(m => uniqueMunicipalityIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            var missingIds = uniqueMunicipalityIds.Except(existingIds).ToList();
            _logger.LogError("Municipality IDs that don't exist in database: {MissingIds}", 
                string.Join(", ", missingIds));

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk insert of {Count} records", records.Count);
            throw;
        }
    }

    /// <summary>
    /// Saves checkpoint after successful batch processing
    /// </summary>
    private async Task SaveCheckpointAsync(
        Domain.Entities.File file,
        int rowNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            file.LastProcessedRowNumber = rowNumber;
            file.LastCheckpointTime = DateTime.UtcNow;

            _fileRepository.Update(file);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Checkpoint saved: Row {RowNumber}", rowNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save checkpoint. Processing will continue.");
            // Don't throw - checkpoint failure shouldn't stop processing
        }
    }

    /// <summary>
    /// Clears checkpoint and job ID after successful completion
    /// </summary>
    private async Task ClearCheckpointAsync(
        Domain.Entities.File file,
        CancellationToken cancellationToken)
    {
        try
        {
            file.LastProcessedRowNumber = null;
            file.LastCheckpointTime = null;
            file.ProcessingJobId = null;

            _fileRepository.Update(file);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Checkpoint and job ID cleared after successful processing");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear checkpoint. This may cause issues on retry.");
            // Don't throw - this is cleanup code
        }
    }
}
