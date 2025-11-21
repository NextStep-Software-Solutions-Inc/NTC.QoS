using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Attributes;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessCellJob : IProcessCellJob
{
    private readonly ILogger<ProcessCellJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.Cell> _cellRepository;
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository;
    private readonly IRepository<Domain.Entities.Sector> _sectorRepository;
    private readonly ITechnologyRepository _technologyRepository;
    private readonly IBandRepository _bandRepository;
    private readonly INetworkRepository _networkRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<Domain.Entities.SystemType> _systemTypeRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserContextService _userContextService;

    // Batch processing configuration
    private const int BATCH_SIZE = 500; // Process 500 cell records per batch
    private const int DUPLICATE_LOAD_BATCH_SIZE = 10000; // Load existing records in batches for memory efficiency

    // Default values for frequency and bandwidth when not specified
    private const decimal DEFAULT_DL_CENTER_FREQ = 0;
    private const decimal DEFAULT_UL_CENTER_FREQ = 0;
    private const decimal DEFAULT_BANDWIDTH = 20; // 20 MHz default bandwidth

    public ProcessCellJob(
        ILogger<ProcessCellJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Cell> cellRepository,
        IRepository<Domain.Entities.Cellular> cellularRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.CellSector> cellSectorRepository,
        IRepository<Domain.Entities.Sector> sectorRepository,
        ITechnologyRepository technologyRepository,
        IBandRepository bandRepository,
        INetworkRepository networkRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<Domain.Entities.SystemType> systemTypeRepository,
        IBlobStorageService blobStorageService,
        IUserContextService userContextService
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _cellRepository = cellRepository;
        _cellularRepository = cellularRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _cellSectorRepository = cellSectorRepository;
        _sectorRepository = sectorRepository;
        _technologyRepository = technologyRepository;
        _bandRepository = bandRepository;
        _networkRepository = networkRepository;
        _municipalityRepository = municipalityRepository;
        _systemTypeRepository = systemTypeRepository;
        _blobStorageService = blobStorageService;
        _userContextService = userContextService;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        var processingResults = new ProcessingResults();

        // Get user context once for all batches
        var userId = _userContextService.GetCurrentUserId() ?? "System";
        var now = DateTime.UtcNow;

        // --- Step 1: Initial File Status Update ---
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(
            parameters.Id, _fileRepository, _logger, FileTypeEnum.Cell, token);

        await ProcessingJobHelper.SetFileProcessingStatusAsync(
            file, _fileRepository, _logger, FileTypeEnum.Cell, token);

        try
        {
            _logger.LogInformation("Starting cell processing for file: {FileName}", file.Name);

            // --- Step 2: Download file from blob storage ---
            Stream fileStream;
            try
            {
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(
                    parameters.Path, _blobStorageService, _logger, FileTypeEnum.Cell, token);
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.Cell, token);
                return;
            }

            stopwatch.Stop();
            _logger.LogInformation("File download completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            using (fileStream)
            {
                stopwatch.Restart();

                // --- Step 3: Pre-load Static/Reference Data for Performance ---
                _logger.LogInformation("Pre-loading reference data.");

                // Pre-load all wireless service providers
                var serviceProviderCache = await _serviceProviderRepository
                    .Query(sp => sp.Service.Type == ServiceTypeEnum.Wireless)
                    .Select(sp => new { sp.Id, sp.Provider.Name })
                    .ToDictionaryAsync(
                        sp => CacheKeyGenerator.GenerateServiceProviderCacheKey(sp.Name, ServiceTypeEnum.Wireless),
                        sp => sp.Id,
                        token
                    );
                _logger.LogInformation("Pre-loaded {Count} wireless service providers.", serviceProviderCache.Count);

                // Pre-load technologies
                var technologyList = await _technologyRepository
                    .QueryAsNoTracking()
                    .Select(t => new { t.Id, Technology = t.Standard })
                    .ToListAsync(token);

                var technologyCache = new Dictionary<string, Guid>();
                foreach (var tech in technologyList)
                {
                    var key = CacheKeyGenerator.GenerateTechnologyCacheKey(tech.Technology);
                    technologyCache[key] = tech.Id;
                }
                _logger.LogInformation("Pre-loaded {Count} technologies.", technologyCache.Count);

                // Pre-load bands
                var bandList = await _bandRepository
                    .QueryAsNoTracking()
                    .Select(b => new { b.Id, Band = b.Name })
                    .ToListAsync(token);

                var bandCache = new Dictionary<string, Guid>();
                foreach (var band in bandList)
                {
                    var key = CacheKeyGenerator.GenerateBandCacheKey(band.Band);
                    bandCache[key] = band.Id;
                }
                _logger.LogInformation("Pre-loaded {Count} bands.", bandCache.Count);

                // NOTE: Municipality boundaries are NOT pre-loaded to avoid memory issues
                // Instead, we use database queries per location with spatial indexes
                _logger.LogInformation("Municipality lookup will use database spatial queries (optimized with indexes).");

                var providerIds = serviceProviderCache.Values.ToList();

                // Pre-load existing cells for cache using hash-based duplicate detection (OPTIMIZED)
                var cellLookup = new HashSet<int>();
                var cellIdMapping = new Dictionary<int, Guid>(); // Maps hash to cell ID
                var totalLoadedCells = 0;
                var totalCells = await _cellRepository
                    .Query(c => providerIds.Contains(c.ServiceProviderId))
                    .CountAsync(token);

                _logger.LogInformation("Loading {TotalCount} existing cells in batches of {BatchSize}...",
                    totalCells, DUPLICATE_LOAD_BATCH_SIZE);

                // Load existing cells in batches to avoid memory overload
                var skip = 0;
                while (true)
                {
                    var existingBatch = await _cellRepository
                        .Query(c => providerIds.Contains(c.ServiceProviderId))
                        .OrderBy(c => c.Id)
                        .Skip(skip)
                        .Take(DUPLICATE_LOAD_BATCH_SIZE)
                        .Select(c => new
                        {
                            c.Id,
                            c.ServiceProviderId,
                            c.TowerType,
                            c.Location
                        })
                        .ToListAsync(token);

                    if (existingBatch.Count == 0)
                        break;

                    foreach (var cell in existingBatch)
                    {
                        var normalizedTowerType = string.IsNullOrWhiteSpace(cell.TowerType) ? "Unknown" : cell.TowerType;
                        var hash = GenerateCellHash(cell.ServiceProviderId, normalizedTowerType, cell.Location);

                        if (cellLookup.Add(hash))
                        {
                            cellIdMapping[hash] = cell.Id;
                        }
                    }

                    totalLoadedCells += existingBatch.Count;
                    skip += DUPLICATE_LOAD_BATCH_SIZE;

                    _logger.LogInformation("Loaded {LoadedCount}/{TotalCount} existing cells...",
                        totalLoadedCells, totalCells);
                }

                _logger.LogInformation("Pre-loaded {Count} unique cells.", cellLookup.Count);

                // Pre-load existing cell sectors
                var cellSectorCache = await _cellSectorRepository
                    .QueryAsNoTracking()
                    .GroupBy(cs => cs.CellId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Select(cs => cs.Id).ToList(),
                        token
                    );
                _logger.LogInformation("Pre-loaded {Count} cell sectors mapped to cells.", cellSectorCache.Count);

                // Pre-load existing Cellular records using hash-based duplicate detection (OPTIMIZED)
                var cellularLookup = new HashSet<int>();
                var totalLoadedCellular = 0;
                var totalCellular = await _cellularRepository
                    .Query(c => serviceProviderCache.Values.Contains(c.ServiceProviderId))
                    .CountAsync(token);

                _logger.LogInformation("Loading {TotalCount} existing cellular records in batches of {BatchSize}...",
                    totalCellular, DUPLICATE_LOAD_BATCH_SIZE);

                skip = 0;
                while (true)
                {
                    var existingCellularBatch = await _cellularRepository
                        .Query(c => serviceProviderCache.Values.Contains(c.ServiceProviderId))
                        .OrderBy(c => c.Id)
                        .Skip(skip)
                        .Take(DUPLICATE_LOAD_BATCH_SIZE)
                        .Select(c => new
                        {
                            c.CellSectorId,
                            c.ServiceProviderId,
                            c.TechnologyId,
                            c.BandId
                        })
                        .ToListAsync(token);

                    if (existingCellularBatch.Count == 0)
                        break;

                    foreach (var cellular in existingCellularBatch)
                    {
                        var hash = GenerateCellularHash(
                            cellular.CellSectorId,
                            cellular.ServiceProviderId,
                            cellular.TechnologyId,
                            cellular.BandId);

                        cellularLookup.Add(hash);
                    }

                    totalLoadedCellular += existingCellularBatch.Count;
                    skip += DUPLICATE_LOAD_BATCH_SIZE;

                    _logger.LogInformation("Loaded {LoadedCount}/{TotalCount} existing cellular records...",
                        totalLoadedCellular, totalCellular);
                }

                _logger.LogInformation("Pre-loaded {Count} unique cellular records.", cellularLookup.Count);

                // Pre-load existing networks
                var networkCache = new Dictionary<string, Guid>();
                var existingNetworks = await _networkRepository
                    .QueryAsNoTracking(n => serviceProviderCache.Values.Contains(n.ServiceProviderId))
                    .Select(n => new { n.Id, n.Name, n.ServiceProviderId, n.SystemType.TechnologyId })
                    .ToListAsync(token);

                foreach (var network in existingNetworks)
                {
                    var nameParts = network.Name.Split('-');
                    if (nameParts.Length >= 3)
                    {
                        var techName = nameParts[0];
                        var bandName = nameParts[1];
                        var municipalityName = nameParts[2];

                        var bandCacheKey = CacheKeyGenerator.GenerateBandCacheKey(bandName);
                        if (bandCache.TryGetValue(bandCacheKey, out var bandId))
                        {
                            var networkKey = CacheKeyGenerator.GenerateNetworkCacheKey(
                                network.ServiceProviderId, municipalityName, network.TechnologyId, bandId);

                            if (!networkCache.ContainsKey(networkKey))
                            {
                                networkCache[networkKey] = network.Id;
                            }
                        }
                    }
                }
                _logger.LogInformation("Pre-loaded {Count} existing networks.", networkCache.Count);

                // Pre-load SystemTypes - handle potential duplicates by taking the first occurrence
                var systemTypeList = await _systemTypeRepository
                    .QueryAsNoTracking()
                    .Select(st => new { st.Id, st.TechnologyId })
                    .ToListAsync(token);

                var systemTypeCache = new Dictionary<Guid, Guid>();
                foreach (var systemType in systemTypeList.Where(st => !systemTypeCache.ContainsKey(st.TechnologyId)))
                {
                    systemTypeCache[systemType.TechnologyId] = systemType.Id;
                }
                _logger.LogInformation("Pre-loaded {Count} system types.", systemTypeCache.Count);

                stopwatch.Stop();
                _logger.LogInformation("Pre-loading reference data completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

                // --- Step 4: Process the Excel File ---
                _logger.LogInformation("Excel processing started");

                var cellRecords = CellExcelHelper.ParseExcelToCellRecords(fileStream);

                if (cellRecords.Count is 0)
                {
                    _logger.LogInformation("The Excel is empty.");
                    await ProcessingJobHelper.SetFileSuccessStatusAsync(
                        file, _fileRepository, _logger, FileTypeEnum.Cell, token);
                    return;
                }

                _logger.LogInformation("Parsed {RecordCount:N0} records from Excel file", cellRecords.Count);
                processingResults.TotalRecords = cellRecords.Count;

                var bandTechnologyProperties = typeof(CellRecord).GetProperties()
                    .Where(p => p.PropertyType == typeof(string))
                    .Select(property => new BandTechnologyPropertyInfo
                    {
                        Property = property,
                        HeaderAttribute = property.GetCustomAttribute<ExcelHeaderAttribute>(),
                        Band = CellPropertyNameParser.GetBandFromPropertyName(property.Name),
                        Technology = CellPropertyNameParser.GetTechnologyFromPropertyName(property.Name)
                    })
                    .Where(item => item.HeaderAttribute != null &&
                                   !string.IsNullOrEmpty(item.Band) &&
                                   !string.IsNullOrEmpty(item.Technology))
                    .ToList();

                _logger.LogInformation("Extracted {Count} band/technology properties from Excel.",
                    bandTechnologyProperties.Count);

                // --- Step 4a: Pre-validate Excel data for records without active technologies ---
                _logger.LogInformation("Validating Excel data for completeness...");

                var recordsWithoutActiveTechnologies = 0;
                foreach (var record in cellRecords)
                {
                    if (!HasAnyActiveTechnology(record, bandTechnologyProperties))
                    {
                        recordsWithoutActiveTechnologies++;
                    }
                }

                processingResults.RecordsWithoutActiveTechnologies = recordsWithoutActiveTechnologies;

                if (recordsWithoutActiveTechnologies > 0)
                {
                    _logger.LogWarning(
                        "⚠️  Pre-validation Warning: Found {Count:N0} records ({Percentage:F1}%) with ALL technology/band columns set to FALSE or empty. " +
                        "These records will create Cells and CellSectors but NOT Cellular/Network/Sector records. " +
                        "If this is unexpected, please review the Excel file before continuing.",
                        recordsWithoutActiveTechnologies,
                        (double)recordsWithoutActiveTechnologies / cellRecords.Count * 100);
                    
                    _logger.LogInformation(
                        "ℹ️  Impact: {Count:N0} CellSectors will be created without coverage data (cannot be used for connectivity analysis).",
                        recordsWithoutActiveTechnologies);
                }
                else
                {
                    _logger.LogInformation("✓ All records have at least one active technology. Data validation passed.");
                }

                // --- Step 5: Process Records in Batches (OPTIMIZED) ---
                var totalRecords = cellRecords.Count;
                var totalBatches = (int)Math.Ceiling((double)totalRecords / BATCH_SIZE);

                var cellSectorsCreatedForCellId = new Dictionary<Guid, List<Guid>>();
                var networksToAdd = new List<Domain.Aggregates.Network>();

                _logger.LogInformation("Processing {TotalRecords:N0} records in {TotalBatches} batches of {BatchSize}",
                    totalRecords, totalBatches, BATCH_SIZE);

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var batch = cellRecords.Skip(batchIndex * BATCH_SIZE).Take(BATCH_SIZE).ToList();
                    var batchNumber = batchIndex + 1;

                    _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({RecordCount} records)",
                        batchNumber, totalBatches, batch.Count);

                    var batchResults = await ProcessBatchAsync(
                        batch,
                        batchIndex * BATCH_SIZE + 4, // Starting row number (Excel rows start at 4)
                        serviceProviderCache,
                        technologyCache,
                        bandCache,
                        cellLookup,
                        cellIdMapping,
                        cellSectorCache,
                        cellularLookup,
                        networkCache,
                        systemTypeCache,
                        bandTechnologyProperties,
                        cellSectorsCreatedForCellId,
                        networksToAdd,
                        processingResults,
                        token);

                    // Save batch results
                    if (batchResults.CellsToAdd.Count > 0)
                    {
                        // CRITICAL: Set audit properties before BulkInsertAsync
                        foreach (var cell in batchResults.CellsToAdd)
                        {
                            cell.CreatedAt = now;
                            cell.CreatedBy = userId;
                            cell.UpdatedAt = now;
                            cell.UpdatedBy = userId;
                        }

                        await _cellRepository.BulkInsertAsync(batchResults.CellsToAdd, token);
                        processingResults.CellsCreated += batchResults.CellsToAdd.Count;
                    }

                    if (batchResults.CellSectorsToAdd.Count > 0)
                    {
                        // CRITICAL: Set audit properties before BulkInsertAsync
                        foreach (var cellSector in batchResults.CellSectorsToAdd)
                        {
                            cellSector.CreatedAt = now;
                            cellSector.CreatedBy = userId;
                            cellSector.UpdatedAt = now;
                            cellSector.UpdatedBy = userId;
                        }

                        await _cellSectorRepository.BulkInsertAsync(batchResults.CellSectorsToAdd, token);
                        processingResults.CellSectorsCreated += batchResults.CellSectorsToAdd.Count;

                        // Update cellSectorCache with newly created sectors
                        foreach (var sector in batchResults.CellSectorsToAdd)
                        {
                            if (!cellSectorCache.ContainsKey(sector.CellId))
                                cellSectorCache[sector.CellId] = [];
                            cellSectorCache[sector.CellId].Add(sector.Id);
                        }
                    }

                    // Save networks accumulated in this batch BEFORE saving Cellular records that reference them
                    if (networksToAdd.Count > 0)
                    {
                        // CRITICAL: Set audit properties before BulkInsertAsync
                        foreach (var network in networksToAdd)
                        {
                            network.CreatedAt = now;
                            network.CreatedBy = userId;
                            network.UpdatedAt = now;
                            network.UpdatedBy = userId;
                        }

                        await _networkRepository.BulkInsertAsync(networksToAdd, token);
                        processingResults.NetworksCreated += networksToAdd.Count;
                        _logger.LogInformation("Batch {BatchNumber}: Saved {Count:N0} new networks to database.",
                            batchNumber, networksToAdd.Count);

                        // Clear the list for the next batch
                        networksToAdd.Clear();
                    }

                    if (batchResults.CellularToAdd.Count > 0)
                    {
                        // CRITICAL: Set audit properties before BulkInsertAsync
                        foreach (var cellular in batchResults.CellularToAdd)
                        {
                            cellular.CreatedAt = now;
                            cellular.CreatedBy = userId;
                            cellular.UpdatedAt = now;
                            cellular.UpdatedBy = userId;
                        }

                        await _cellularRepository.BulkInsertAsync(batchResults.CellularToAdd, token);
                        processingResults.CellularCreated += batchResults.CellularToAdd.Count;
                    }

                    if (batchResults.CellularToDelete.Count > 0)
                    {
                        await _cellularRepository.BulkDeleteAsync(batchResults.CellularToDelete);
                        processingResults.CellularDeleted += batchResults.CellularToDelete.Count;
                    }

                    // Detailed progress reporting (like Wireless Validation)
                    var processedCount = Math.Min((batchIndex + 1) * BATCH_SIZE, totalRecords);
                    var progress = (double)processedCount / totalRecords * 100;
                    ProcessingJobHelper.ReportProgress(
                        $"Batch {batchNumber}/{totalBatches} completed - Progress: {progress:F1}% - " +
                        $"Successful: {processingResults.SuccessfulRecords:N0}, Errors: {processingResults.ErrorRecords:N0}, " +
                        $"Cells Created: {processingResults.CellsCreated:N0}, Cellular Created: {processingResults.CellularCreated:N0}, " +
                        $"Duplicates Skipped: {processingResults.SkippedDuplicates:N0}",
                        _logger);
                }

                // --- Step 6: All networks have been saved per-batch, this section is now for final checks only ---
                _logger.LogInformation("All batches processed. Networks were saved per-batch before Cellular records.");

                // --- Step 7: Create Sector entities for CellSectors without existing Sectors (handles both new and reprocessing) ---
                _logger.LogInformation("Creating Sector entities for CellSectors without existing Sectors...");

                // Get all CellSector IDs from this processing run (both new and existing cells)
                var allProcessedCellIds = new HashSet<Guid>();

                // Add newly created cells
                foreach (var cellId in cellSectorsCreatedForCellId.Keys)
                {
                    allProcessedCellIds.Add(cellId);
                }

                // Add existing cells that were processed (from cellIdMapping)
                foreach (var cellId in cellIdMapping.Values)
                {
                    allProcessedCellIds.Add(cellId);
                }

                if (allProcessedCellIds.Count > 0)
                {
                    // Get all CellSector IDs for processed cells
                    var allCellSectorIds = await _cellSectorRepository
                        .QueryAsNoTracking(cs => allProcessedCellIds.Contains(cs.CellId))
                        .Select(cs => cs.Id)
                        .ToListAsync(token);

                    _logger.LogInformation("Found {Count:N0} CellSectors from {CellCount:N0} processed cells.",
                        allCellSectorIds.Count, allProcessedCellIds.Count);

                    // Check which CellSectors already have Sectors
                    var existingSectorIds = await _sectorRepository
                        .QueryAsNoTracking(s => allCellSectorIds.Contains(s.CellSectorId))
                        .Select(s => s.CellSectorId)
                        .ToListAsync(token);

                    var existingSet = existingSectorIds.ToHashSet();

                    _logger.LogInformation("{ExistingCount:N0} CellSectors already have Sectors.",
                        existingSet.Count);

                    // Get CellSector details for sectors we need to create
                    var cellSectorsNeedingSectors = await _cellSectorRepository
                        .QueryAsNoTracking(cs => allCellSectorIds.Contains(cs.Id) &&
                                                 !existingSet.Contains(cs.Id))
                        .ToListAsync(token);

                    _logger.LogInformation("{NeedingSectorsCount:N0} CellSectors need Sector creation.",
                        cellSectorsNeedingSectors.Count);

                    if (cellSectorsNeedingSectors.Count > 0)
                    {
                        var sectorsToAdd = new List<Domain.Entities.Sector>();

                        // Load all cellular records to determine primary network for each CellSector
                        var cellularForSectors = await _cellularRepository
                            .QueryAsNoTracking(c => cellSectorsNeedingSectors.Select(cs => cs.Id).Contains(c.CellSectorId))
                            .Select(c => new { c.CellSectorId, c.NetworkId })
                            .GroupBy(c => c.CellSectorId)
                            .ToDictionaryAsync(
                                g => g.Key,
                                g => g.First().NetworkId,  // Use first (primary) network
                                token);

                        _logger.LogInformation("Found Cellular records for {Count:N0} CellSectors.",
                            cellularForSectors.Count);

                        var skippedSectorsCount = 0;
                        foreach (var cellSector in cellSectorsNeedingSectors)
                        {
                            // Get NetworkId from cellular records, default to Guid.Empty if not found
                            var networkId = cellularForSectors.TryGetValue(cellSector.Id, out var nId)
                                ? nId
                                : Guid.Empty;

                            if (networkId == Guid.Empty)
                            {
                                _logger.LogWarning("Skipping Sector creation for CellSector {CellSectorId} - No valid Network found. " +
                                    "This may indicate missing SystemType configuration or Cellular records not yet created.", cellSector.Id);
                                skippedSectorsCount++;
                                continue;
                            }

                            var baseSector = new Domain.Entities.Sector
                            {
                                CellSectorId = cellSector.Id,
                                NetworkId = networkId,
                                AntennaType = cellSector.AntennaType,
                                TxAntennaHeight = cellSector.AntennaHeight,
                                TxAzimuth = cellSector.Azimuth,
                                TxBeamTilt = cellSector.BeamTilt,
                                RxAntennaHeight = cellSector.AntennaHeight,
                                RxAzimuth = cellSector.Azimuth,
                                RxBeamTilt = cellSector.BeamTilt,
                                IsRxEqualTx = true,
                                CreatedAt = now,
                                CreatedBy = userId,
                                UpdatedAt = now,
                                UpdatedBy = userId
                            };

                            sectorsToAdd.Add(baseSector);
                        }

                        if (sectorsToAdd.Count > 0)
                        {
                            await _sectorRepository.BulkInsertAsync(sectorsToAdd, token);
                            processingResults.SectorsCreated = sectorsToAdd.Count;
                            _logger.LogInformation("Created {Count:N0} sector entities for CellSectors.",
                                sectorsToAdd.Count);
                        }

                        if (skippedSectorsCount > 0)
                        {
                            processingResults.CellSectorsWithoutCellular += skippedSectorsCount;
                            
                            _logger.LogWarning("Skipped {Count:N0} sectors due to missing or invalid NetworkId. " +
                                "Possible reasons: " +
                                "1) All technology/band columns in Excel were FALSE/empty for these CellSectors, " +
                                "2) Missing SystemType configuration for required technologies, " +
                                "3) Cellular records failed to create due to other errors. " +
                                "Check the Excel file and ensure at least one technology/band is set to TRUE or has valid CF/BW values.",
                                skippedSectorsCount);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("All CellSectors already have Sectors. No new sectors to create.");
                    }
                }

                // --- Step 8: Complete Processing with Summary ---
                await CompleteProcessing(file, processingResults, startTime, token);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error occurred during cell processing after {ElapsedTime}: {ErrorMessage}",
                stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"), ex.Message);
            await ProcessingJobHelper.TerminateProcessAsync(
                file, _fileRepository, _logger, FileTypeEnum.Cell,
                $"Error occurred during processing: {ex.Message}", token);
            throw;
        }
    }

    /// <summary>
    /// Completes processing and logs comprehensive summary (like Wireless Validation)
    /// </summary>
    private async Task CompleteProcessing(
        Domain.Entities.File file,
        ProcessingResults results,
        DateTime startTime,
        CancellationToken token)
    {
        var processingTime = DateTime.UtcNow - startTime;
        var successRate = results.TotalRecords > 0
            ? (double)results.SuccessfulRecords / results.TotalRecords * 100
            : 0;

        _logger.LogInformation("=== Cell Processing Summary ===");
        _logger.LogInformation("File: {FileName}", file.Name);
        _logger.LogInformation("Processing Time: {ProcessingTime:hh\\:mm\\:ss}", processingTime);
        _logger.LogInformation("Total Records: {TotalRecords:N0}", results.TotalRecords);
        _logger.LogInformation("Processed Records: {ProcessedRecords:N0}", results.ProcessedRecords);
        _logger.LogInformation("Successful Records: {SuccessfulRecords:N0} ({SuccessRate:F1}%)",
            results.SuccessfulRecords, successRate);
        _logger.LogInformation("Skipped Records: {SkippedRecords:N0}", results.SkippedRecords);
        _logger.LogInformation("Error Records: {ErrorRecords:N0}", results.ErrorRecords);

        _logger.LogInformation("Entities Created:");
        _logger.LogInformation("  Cells: {CellsCreated:N0}", results.CellsCreated);
        _logger.LogInformation("  Cell Sectors: {CellSectorsCreated:N0}", results.CellSectorsCreated);
        _logger.LogInformation("  Cellular Records: {CellularCreated:N0}", results.CellularCreated);
        _logger.LogInformation("  Networks: {NetworksCreated:N0}", results.NetworksCreated);
        _logger.LogInformation("  Sectors: {SectorsCreated:N0}", results.SectorsCreated);
        _logger.LogInformation("  Cellular Deleted: {CellularDeleted:N0}", results.CellularDeleted);

        _logger.LogInformation("Duplicates Skipped:");
        _logger.LogInformation("  Total Duplicates: {SkippedDuplicates:N0}", results.SkippedDuplicates);

        // Enhanced validation statistics
        if (results.CellSectorsWithoutCellular > 0 || results.RecordsWithoutActiveTechnologies > 0)
        {
            _logger.LogWarning("⚠️  Data Quality Issues Detected:");
            
            if (results.RecordsWithoutActiveTechnologies > 0)
            {
                _logger.LogWarning(
                    "  • {Count:N0} records had NO active technologies (all tech/band columns were FALSE/empty)",
                    results.RecordsWithoutActiveTechnologies);
            }
            
            if (results.CellSectorsWithoutCellular > 0)
            {
                _logger.LogWarning(
                    "  • {Count:N0} CellSectors were created WITHOUT Cellular/Sector records",
                    results.CellSectorsWithoutCellular);
                _logger.LogWarning(
                    "  • These CellSectors CANNOT be used for coverage analysis until Cellular records are added");
                _logger.LogWarning(
                    "  • Action Required: Review Excel file and ensure technology/band columns have TRUE or valid 'CF: x, BW: y' values");
            }
        }

        if (results.TotalRecords > 0)
        {
            var processingRate = results.TotalRecords / processingTime.TotalSeconds;
            _logger.LogInformation("Processing Rate: {ProcessingRate:F0} records/second", processingRate);

            var avgCellularPerRecord = results.SuccessfulRecords > 0
                ? (double)results.CellularCreated / results.SuccessfulRecords
                : 0;
            _logger.LogInformation("Average Cellular per Record: {AvgCellular:F1}", avgCellularPerRecord);

            var totalPotentialCellular = results.CellularCreated + results.SkippedDuplicates;
            var duplicateRate = totalPotentialCellular > 0
                ? (double)results.SkippedDuplicates / totalPotentialCellular * 100
                : 0;
            _logger.LogInformation("Duplicate Rate: {DuplicateRate:F1}% ({SkippedDuplicates:N0} of {TotalPotential:N0} potential cellular)",
                duplicateRate, results.SkippedDuplicates, totalPotentialCellular);
        }

        _logger.LogInformation("Processing Logic Applied:");
        _logger.LogInformation("- Hash-based duplicate detection (4-byte hashes)");
        _logger.LogInformation("- Batched duplicate loading (10K chunks)");
        _logger.LogInformation("- Batch processing (500 records per batch)");
        _logger.LogInformation("- Coordinate validation and correction");
        _logger.LogInformation("- Hierarchical cell -> sector -> cellular creation");
        _logger.LogInformation("- Network auto-creation with SystemType validation");
        _logger.LogInformation("- 1:1 Sector to CellSector ratio");
        _logger.LogInformation("=======================================\n");

        file.Status = results.ErrorRecords == 0 ? FileStatusEnum.Success : FileStatusEnum.PartiallyProcessed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        _logger.LogInformation("Processing completed successfully.");
    }

    /// <summary>
    /// Processes a batch of cell records and returns entities to be saved
    /// </summary>
    private async Task<BatchProcessingResult> ProcessBatchAsync(
        List<CellRecord> batch,
        int startingRowNumber,
        Dictionary<string, Guid> serviceProviderCache,
        Dictionary<string, Guid> technologyCache,
        Dictionary<string, Guid> bandCache,
        HashSet<int> cellLookup,
        Dictionary<int, Guid> cellIdMapping,
        Dictionary<Guid, List<Guid>> cellSectorCache,
        HashSet<int> cellularLookup,
        Dictionary<string, Guid> networkCache,
        Dictionary<Guid, Guid> systemTypeCache,
        List<BandTechnologyPropertyInfo> bandTechnologyProperties,
        Dictionary<Guid, List<Guid>> cellSectorsCreatedForCellId,
        List<Domain.Aggregates.Network> networksToAdd,
        ProcessingResults processingResults,
        CancellationToken token)
    {
        var result = new BatchProcessingResult();

        for (int i = 0; i < batch.Count; i++)
        {
            var record = batch[i];
            var actualRowNumber = startingRowNumber + i;

            try
            {
                processingResults.ProcessedRecords++;

                var address = BuildAddress(record);

                var spCacheKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(
                    record.Provider, ServiceTypeEnum.Wireless);

                if (!serviceProviderCache.TryGetValue(spCacheKey, out var serviceProviderId))
                {
                    _logger.LogWarning("Wireless Service Provider not found: {Provider}. Skipping record at row {Row}.",
                        record.Provider, actualRowNumber);
                    processingResults.SkippedRecords++;
                    continue;
                }

                // Handle coordinate swapping if needed
                var actualLatitude = record.Latitude;
                var actualLongitude = record.Longitude;

                if (record.Latitude > 50 && record.Longitude < 50)
                {
                    actualLatitude = record.Longitude;
                    actualLongitude = record.Latitude;
                }

                var location = GeometryHelper.CreatePoint(new Coordinate(y: actualLatitude, x: actualLongitude));
                var normalizedTowerType = string.IsNullOrWhiteSpace(record.TowerType) ? "Unknown" : record.TowerType;

                // Check if cell exists using hash
                var cellHash = GenerateCellHash(serviceProviderId, normalizedTowerType, location);
                Guid cellId;
                bool isNewCell = false;

                if (!cellLookup.Contains(cellHash))
                {
                    // Create new cell
                    var cell = new Domain.Aggregates.Cell
                    {
                        Name = $"Cell-{record.Provider}-{address}-{record.TowerType}-{record.Longitude}-{record.Latitude}",
                        Group = record.Provider,
                        Address = address,
                        ServiceProviderId = serviceProviderId,
                        TowerType = normalizedTowerType,
                        Location = location,
                        IsFileUploaded = true,
                    };

                    result.CellsToAdd.Add(cell);
                    cellId = cell.Id;
                    cellLookup.Add(cellHash);
                    cellIdMapping[cellHash] = cellId;
                    isNewCell = true;
                }
                else
                {
                    // Cell exists, reuse it
                    cellId = cellIdMapping[cellHash];
                    isNewCell = false;
                }

                // Process cell sectors for new cell OR existing cell without sectors (reprocessing scenario)
                bool shouldCreateCellSectors = isNewCell;
                
                // Check if existing cell has CellSectors
                if (!isNewCell)
                {
                    // Check both cache sources for existing CellSectors
                    bool hasExistingSectors = cellSectorCache.TryGetValue(cellId, out var existingSectors) && existingSectors.Count > 0;
                    bool hasNewlyCreatedSectors = cellSectorsCreatedForCellId.TryGetValue(cellId, out var newSectors) && newSectors.Count > 0;
                    
                    if (!hasExistingSectors && !hasNewlyCreatedSectors)
                    {
                        _logger.LogInformation(
                            "Row {Row}: Existing Cell {CellId} has no CellSectors. " +
                            "Will attempt to create CellSectors from current row data (reprocessing scenario).",
                            actualRowNumber, cellId);
                        shouldCreateCellSectors = true;
                    }
                }

                if (shouldCreateCellSectors)
                {
                    if (!string.IsNullOrWhiteSpace(record.Azimuth) && !string.IsNullOrWhiteSpace(record.BeamTilt))
                    {
                        var azimuths = record.Azimuth.Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim())
                            .Where(a => !string.IsNullOrWhiteSpace(a))
                            .ToArray();
                        
                        var beamTilts = record.BeamTilt.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(b => b.Trim())
                            .Where(b => !string.IsNullOrWhiteSpace(b))
                            .ToArray();

                        if (azimuths.Length > 0 && beamTilts.Length > 0)
                        {
                            if (azimuths.Length != beamTilts.Length)
                            {
                                _logger.LogWarning(
                                    "Azimuth/BeamTilt count mismatch at row {Row}. " +
                                    "Azimuth count: {AzimuthCount}, BeamTilt count: {BeamTiltCount}. " +
                                    "Azimuth values: '{Azimuth}', BeamTilt values: '{BeamTilt}'. " +
                                    "Skipping CellSector creation for this cell.",
                                    actualRowNumber, 
                                    azimuths.Length, 
                                    beamTilts.Length,
                                    record.Azimuth,
                                    record.BeamTilt);
                            }
                            else
                            {
                                // Create CellSectors only when counts match
                                for (int j = 0; j < azimuths.Length; j++)
                                {
                                    if (decimal.TryParse(azimuths[j], out decimal azimuth) &&
                                        decimal.TryParse(beamTilts[j], out decimal beamTilt))
                                    {
                                        var cellSector = new Domain.Entities.CellSector
                                        {
                                            CellId = cellId,
                                            AntennaHeight = record.AntennaHeight,
                                            AntennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(record.AntennaType),
                                            Azimuth = azimuth,
                                            BeamTilt = beamTilt,
                                        };

                                        result.CellSectorsToAdd.Add(cellSector);

                                        if (!cellSectorsCreatedForCellId.ContainsKey(cellId))
                                            cellSectorsCreatedForCellId[cellId] = [];
                                        cellSectorsCreatedForCellId[cellId].Add(cellSector.Id);
                                    }
                                    else
                                    {
                                        _logger.LogWarning(
                                            "Failed to parse Azimuth/BeamTilt values at row {Row}, sector index {Index}. " +
                                            "Azimuth: '{Azimuth}', BeamTilt: '{BeamTilt}'. Skipping this sector.",
                                            actualRowNumber,
                                            j,
                                            azimuths[j],
                                            beamTilts[j]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Row {Row}: No valid azimuth or beamtilt values found. " +
                                "Azimuth: '{Azimuth}', BeamTilt: '{BeamTilt}'. Skipping CellSector creation.",
                                actualRowNumber, record.Azimuth, record.BeamTilt);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(record.Azimuth) && string.IsNullOrWhiteSpace(record.BeamTilt))
                        {
                            _logger.LogWarning("Row {Row}: Both Azimuth and BeamTilt columns are empty. Skipping CellSector creation.",
                                actualRowNumber);
                        }
                        else if (string.IsNullOrWhiteSpace(record.Azimuth))
                        {
                            _logger.LogWarning("Row {Row}: Azimuth column is empty (BeamTilt: '{BeamTilt}'). Skipping CellSector creation.",
                                actualRowNumber, record.BeamTilt);
                        }
                        else
                        {
                            _logger.LogWarning("Row {Row}: BeamTilt column is empty (Azimuth: '{Azimuth}'). Skipping CellSector creation.",
                                actualRowNumber, record.Azimuth);
                        }
                    }
                }

                // Process cellular data
                if (cellId != Guid.Empty)
                {
                    await ProcessCellularDataForRecordAsync(
                        record,
                        cellId,
                        serviceProviderId,
                        address,
                        location,
                        actualRowNumber,
                        technologyCache,
                        bandCache,
                        cellSectorCache,
                        cellularLookup,
                        networkCache,
                        systemTypeCache,
                        cellSectorsCreatedForCellId,
                        networksToAdd,
                        bandTechnologyProperties,
                        result,
                        processingResults,
                        token);

                    processingResults.SuccessfulRecords++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing record at row {Row}. Skipping.", actualRowNumber);
                processingResults.ErrorRecords++;
                continue;
            }
        }

        return result;
    }

    /// <summary>
    /// Processes cellular data for a single cell record
    /// </summary>
    private async Task ProcessCellularDataForRecordAsync(
        CellRecord record,
        Guid cellId,
        Guid serviceProviderId,
        string address,
        Point location,
        int rowNumber,
        Dictionary<string, Guid> technologyCache,
        Dictionary<string, Guid> bandCache,
        Dictionary<Guid, List<Guid>> cellSectorCache,
        HashSet<int> cellularLookup,
        Dictionary<string, Guid> networkCache,
        Dictionary<Guid, Guid> systemTypeCache,
        Dictionary<Guid, List<Guid>> cellSectorsCreatedForCellId,
        List<Domain.Aggregates.Network> networksToAdd,
        List<BandTechnologyPropertyInfo> bandTechnologyProperties,
        BatchProcessingResult result,
        ProcessingResults processingResults,
        CancellationToken token)
    {
        foreach (var propInfo in bandTechnologyProperties)
        {
            var technologyId = TechnologyMappingHelper.ResolveTechnologyId(
                propInfo.Technology, technologyCache, _logger, $"Excel row {rowNumber}");

            if (!TechnologyMappingHelper.IsTechnologyResolved(technologyId))
            {
                continue;
            }

            var bandId = TechnologyMappingHelper.ResolveBandId(
                propInfo.Band, bandCache, _logger, $"Excel row {rowNumber}");

            if (!TechnologyMappingHelper.IsBandResolved(bandId))
            {
                continue;
            }

            var value = propInfo.Property.GetValue(record);
            var parseResult = ParseBandTechnologyValue(value, propInfo.Technology, propInfo.Band, rowNumber);

            // Get all sector IDs for this cell
            var allSectorIds = new List<Guid>();
            if (cellSectorCache.TryGetValue(cellId, out var existingSectorIds))
            {
                allSectorIds.AddRange(existingSectorIds);
            }

            if (cellSectorsCreatedForCellId.TryGetValue(cellId, out var newSectorIds))
            {
                allSectorIds.AddRange(newSectorIds);
            }

            if (allSectorIds.Count == 0)
            {
                _logger.LogWarning("Row {Row}: No CellSectors found for Cell {CellId} - Cannot create Cellular records", 
                    rowNumber, cellId);
                continue;
            }

            // Log parse result for debugging
            if (!parseResult.IsActive)
            {
                _logger.LogDebug("Row {Row}: {Technology}/{Band} is not active (value: {Value})", 
                    rowNumber, propInfo.Technology, propInfo.Band, value);
            }

            foreach (var sectorId in allSectorIds)
            {
                var cellularHash = GenerateCellularHash(sectorId, serviceProviderId, technologyId, bandId);

                if (cellularLookup.Contains(cellularHash))
                {
                    processingResults.SkippedDuplicates++;
                    continue;
                }

                if (parseResult.IsActive)
                {
                    // Create new cellular record
                    var dlCenterFreq = parseResult.DLCenterFreq ?? GetDefaultDLCenterFreq(propInfo.Technology, propInfo.Band);
                    var bandwidth = parseResult.Bandwidth ?? GetDefaultBandwidth(propInfo.Technology, propInfo.Band);
                    var ulCenterFreq = CalculateULCenterFreq(dlCenterFreq, propInfo.Technology, propInfo.Band);

                    // Use database query for municipality lookup instead of pre-loaded boundaries
                    var (municipalityId, municipalityName) = await GetMunicipalityByLocationAsync(location, token);

                    var networkId = await GetOrCreateNetworkAsync(
                        serviceProviderId,
                        municipalityName,
                        technologyId,
                        bandId,
                        networkCache,
                        networksToAdd,
                        technologyCache,
                        bandCache,
                        systemTypeCache,
                        dlCenterFreq,
                        ulCenterFreq,
                        bandwidth,
                        token);

                    if (networkId == Guid.Empty)
                    {
                        _logger.LogWarning("Row {Row}: Skipping Cellular for {Technology}/{Band} - NetworkId is Empty. " +
                            "Check SystemType configuration for TechnologyId: {TechnologyId}", 
                            rowNumber, propInfo.Technology, propInfo.Band, technologyId);
                        continue;
                    }

                    var newCellular = new Domain.Entities.Cellular
                    {
                        CellSectorId = sectorId,
                        ServiceProviderId = serviceProviderId,
                        TechnologyId = technologyId,
                        BandId = bandId,
                        NetworkId = networkId,
                        Address = address,
                        IsFileUploaded = true,
                        DLCenterFreq = dlCenterFreq,
                        Bandwidth = bandwidth,
                        ULCenterFreq = ulCenterFreq
                    };

                    result.CellularToAdd.Add(newCellular);
                    cellularLookup.Add(cellularHash);
                    
                    _logger.LogDebug("Row {Row}: Created Cellular record for {Technology}/{Band} with Network {NetworkId}", 
                        rowNumber, propInfo.Technology, propInfo.Band, networkId);
                }
            }
        }
    }

    /// <summary>
    /// Generates a hash code for cell duplicate detection
    /// </summary>
    private static int GenerateCellHash(Guid serviceProviderId, string towerType, Point location)
    {
        var hash = new HashCode();
        hash.Add(serviceProviderId);
        hash.Add(towerType);
        hash.Add(Math.Round(location.Coordinate.X, 6));
        hash.Add(Math.Round(location.Coordinate.Y, 6));
        return hash.ToHashCode();
    }

    /// <summary>
    /// Generates a hash code for cellular duplicate detection
    /// </summary>
    private static int GenerateCellularHash(Guid cellSectorId, Guid serviceProviderId, Guid technologyId, Guid bandId)
    {
        var hash = new HashCode();
        hash.Add(cellSectorId);
        hash.Add(serviceProviderId);
        hash.Add(technologyId);
        hash.Add(bandId);
        return hash.ToHashCode();
    }

    // The remaining helper methods remain unchanged from the original implementation
    // (BuildAddress, ParseBandTechnologyValue, GetDefaultDLCenterFreq, GetDefaultBandwidth, 
    //  CalculateULCenterFreq, GetMunicipalityByLocationAsync, GetOrCreateNetworkAsync, etc.)

    private static string BuildAddress(CellRecord record)
    {
        return $"{record.Zone}, {record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";
    }

    private BandTechnologyParseResult ParseBandTechnologyValue(object? value, string technology, string band, int rowNumber)
    {
        var result = new BandTechnologyParseResult();

        if (value == null)
        {
            result.IsActive = false;
            return result;
        }

        var stringValue = value.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(stringValue))
        {
            result.IsActive = false;
            return result;
        }

        if (stringValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || stringValue == "1")
        {
            result.IsActive = true;
            return result;
        }

        if (stringValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase) || stringValue == "0")
        {
            result.IsActive = false;
            return result;
        }

        var pattern = @"CF:\s*([0-9]*\.?[0-9]+),\s*BW:\s*([0-9]*\.?[0-9]+)";
        var match = Regex.Match(stringValue, pattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            result.IsActive = true;

            if (decimal.TryParse(match.Groups[1].Value, out var dlCenterFreq))
            {
                result.DLCenterFreq = dlCenterFreq;
            }

            if (decimal.TryParse(match.Groups[2].Value, out var bandwidth))
            {
                result.Bandwidth = bandwidth;
            }

            return result;
        }

        result.IsActive = false;
        return result;
    }

    private static decimal GetDefaultDLCenterFreq(string technology, string band)
    {
        return (technology.ToUpperInvariant(), band) switch
        {
            ("GSM", "Band 8") => 947.5m,
            ("GSM", "Band 3") => 1842.5m,
            ("EDGE", "Band 8") => 947.5m,
            ("EDGE", "Band 3") => 1842.5m,
            ("WCDMA", "Band 5") => 869m,
            ("WCDMA", "Band 8") => 950m,
            ("WCDMA", "Band 1") => 2140m,
            ("HSPAP", "Band 5") => 869m,
            ("HSPAP", "Band 8") => 950m,
            ("HSPAP", "Band 1") => 2140m,
            ("FDDLTE", "Band 28") => 758m,
            ("FDDLTE", "Band 5") => 869m,
            ("FDDLTE", "Band 8") => 950m,
            ("FDDLTE", "Band 3") => 1842.5m,
            ("FDDLTE", "Band 1") => 2140m,
            ("LTE", "Band 28") => 758m,
            ("LTE", "Band 5") => 869m,
            ("LTE", "Band 8") => 950m,
            ("LTE", "Band 3") => 1842.5m,
            ("LTE", "Band 1") => 2140m,
            ("TDDLTE", "Band 40") => 2300m,
            ("TDDLTE", "Band 41") => 2500m,
            ("TDDLTE", "Band 38") => 2600m,
            ("NR", "Band 78") => 3500m,
            ("NR", "Band 38") => 2600m,
            var (tech, bandName) when tech.Contains("TDD") => GetTDDDefaultFreq(bandName),
            var (tech, bandName) when tech.Contains("FDD") => GetFDDDefaultFreq(bandName),
            _ => DEFAULT_DL_CENTER_FREQ
        };
    }

    private static decimal GetTDDDefaultFreq(string band)
    {
        return band switch
        {
            "Band 40" => 2300m,
            "Band 41" => 2500m,
            "Band 38" => 2600m,
            "Band 42" => 3400m,
            "Band 78" => 3500m,
            _ => DEFAULT_DL_CENTER_FREQ
        };
    }

    private static decimal GetFDDDefaultFreq(string band)
    {
        return band switch
        {
            "Band 28" => 758m,
            "Band 5" => 869m,
            "Band 8" => 950m,
            "Band 3" => 1842.5m,
            "Band 1" => 2140m,
            _ => DEFAULT_DL_CENTER_FREQ
        };
    }

    private static decimal GetDefaultBandwidth(string technology, string band)
    {
        return technology.ToUpperInvariant() switch
        {
            "GSM" => 0.2m,
            "EDGE" => 0.2m,
            "WCDMA" => 5m,
            "HSPAP" => 5m,
            "FDDLTE" => 20m,
            "TDDLTE" => 20m,
            "LTE" => 20m,
            "NR" => 100m,
            var tech when tech.Contains("TDD") => 20m,
            var tech when tech.Contains("FDD") => 20m,
            _ => DEFAULT_BANDWIDTH
        };
    }

    private decimal CalculateULCenterFreq(decimal dlCenterFreq, string technology, string band)
    {
        if (technology.Contains("TDD", StringComparison.OrdinalIgnoreCase) ||
            (technology.Equals("NR", StringComparison.OrdinalIgnoreCase) && IsTDDBand(band)))
        {
            return dlCenterFreq;
        }

        return technology.ToUpperInvariant() switch
        {
            "GSM" => CalculateGsmULFreq(dlCenterFreq, band),
            "EDGE" => CalculateGsmULFreq(dlCenterFreq, band),
            "WCDMA" => CalculateWcdmaULFreq(dlCenterFreq, band),
            "HSPAP" => CalculateWcdmaULFreq(dlCenterFreq, band),
            "FDDLTE" => CalculateLteFddULFreq(dlCenterFreq, band),
            "LTE" => CalculateLteFddULFreq(dlCenterFreq, band),
            "TDDLTE" => dlCenterFreq,
            "NR" => CalculateNrFddULFreq(dlCenterFreq, band),
            _ => dlCenterFreq
        };
    }

    private static bool IsTDDBand(string band)
    {
        var tddBands = new[] { "Band 38", "Band 40", "Band 41", "Band 42", "Band 78" };
        return tddBands.Contains(band, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal CalculateGsmULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 8" => dlCenterFreq - 45,
            "Band 3" => dlCenterFreq - 95,
            _ => dlCenterFreq - 45
        };
    }

    private static decimal CalculateWcdmaULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 5" => dlCenterFreq - 45,
            "Band 8" => dlCenterFreq - 45,
            "Band 1" => dlCenterFreq - 190,
            _ => dlCenterFreq - 190
        };
    }

    private static decimal CalculateLteFddULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 28" => dlCenterFreq - 55,
            "Band 5" => dlCenterFreq - 45,
            "Band 8" => dlCenterFreq - 45,
            "Band 3" => dlCenterFreq - 95,
            "Band 1" => dlCenterFreq - 190,
            _ => dlCenterFreq - 45
        };
    }

    private static decimal CalculateNrFddULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 28" => dlCenterFreq - 55,
            "Band 1" => dlCenterFreq - 190,
            "Band 3" => dlCenterFreq - 95,
            _ => dlCenterFreq - 45
        };
    }

    /// <summary>
    /// Gets municipality by location using database spatial query with indexes.
    /// OPTIMIZED: Uses database spatial intersection instead of materializing boundaries.
    /// </summary>
    private async Task<(Guid? Id, string Name)> GetMunicipalityByLocationAsync(
        Point location,
        CancellationToken cancellationToken)
    {
        try
        {
            if (location?.IsValid != true)
            {
                return (null, "Unknown");
            }

            // Use database query with spatial index - let PostgreSQL/PostGIS handle intersection
            var municipality = await _municipalityRepository.FirstOrDefaultAsync(
            _municipalityRepository
                .Query(m => m.Boundaries != null && m.Boundaries.Intersects(location))
                .Select(m => new { m.Id, m.Name }), cancellationToken);

            return municipality != null ? (municipality.Id, municipality.Name) : (null, "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up municipality for location: {Location}", location);
            return (null, "Unknown");
        }
    }

    private async Task<Guid> GetOrCreateNetworkAsync(
        Guid serviceProviderId,
        string municipalityName,
        Guid technologyId,
        Guid bandId,
        Dictionary<string, Guid> networkCache,
        List<Domain.Aggregates.Network> networksToAdd,
        Dictionary<string, Guid> technologyCache,
        Dictionary<string, Guid> bandCache,
        Dictionary<Guid, Guid> systemTypeCache,
        decimal dlCenterFreq,
        decimal ulCenterFreq,
        decimal bandwidth,
        CancellationToken token)
    {
        var networkCacheKey = CacheKeyGenerator.GenerateNetworkCacheKey(
        serviceProviderId, municipalityName, technologyId, bandId);

        if (networkCache.TryGetValue(networkCacheKey, out var existingNetworkId))
        {
            return existingNetworkId;
        }

        var technologyName = technologyCache.FirstOrDefault(x => x.Value == technologyId).Key ?? "Unknown";
        var bandName = bandCache.FirstOrDefault(x => x.Value == bandId).Key ?? "Unknown";

        var networkNamePattern = $"{technologyName}-{bandName}-{municipalityName}";
        var existingNetwork = await _networkRepository
            .QueryAsNoTracking(n => n.ServiceProviderId == serviceProviderId && n.Name.StartsWith(networkNamePattern))
            .FirstOrDefaultAsync(token);

        if (existingNetwork != null)
        {
            networkCache[networkCacheKey] = existingNetwork.Id;
            return existingNetwork.Id;
        }

        // Use pre-loaded systemTypeCache instead of database query
        if (!systemTypeCache.TryGetValue(technologyId, out var systemTypeId))
        {
            _logger.LogWarning("SystemType not found for TechnologyId: {TechnologyId} (Technology: {Technology})",
                technologyId, technologyName);
            return Guid.Empty;
        }

        var network = new Domain.Aggregates.Network
        {
            Name = $"{technologyName}-{bandName}-{municipalityName}",
            ServiceProviderId = serviceProviderId,
            SystemTypeId = systemTypeId,  // ✅ From cache
            ULCenterFreq = ulCenterFreq,
            DLCenterFreq = dlCenterFreq,
            Bandwidth = bandwidth
        };

        networksToAdd.Add(network);
        networkCache[networkCacheKey] = network.Id;
        return network.Id;
    }

    /// <summary>
    /// Checks if a cell record has at least one active technology/band combination
    /// </summary>
    private bool HasAnyActiveTechnology(CellRecord record, List<BandTechnologyPropertyInfo> bandTechnologyProperties)
    {
        foreach (var propInfo in bandTechnologyProperties)
        {
            var value = propInfo.Property.GetValue(record);
            var parseResult = ParseBandTechnologyValue(value, propInfo.Technology, propInfo.Band, 0);
            
            if (parseResult.IsActive)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// DTO for band/technology property information extracted from CellRecord
    /// </summary>
    private class BandTechnologyPropertyInfo
    {
        public PropertyInfo Property { get; set; } = null!;
        public ExcelHeaderAttribute? HeaderAttribute { get; set; }
        public string Band { get; set; } = string.Empty;
        public string Technology { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result structure for parsing band/technology column values
    /// </summary>      
    private class BandTechnologyParseResult
    {
        public bool IsActive { get; set; }
        public decimal? DLCenterFreq { get; set; }
        public decimal? Bandwidth { get; set; }
    }

    /// <summary>
    /// Container for batch processing results
    /// </summary>
    private class BatchProcessingResult
    {
        public List<Domain.Aggregates.Cell> CellsToAdd { get; set; } = [];
        public List<Domain.Entities.CellSector> CellSectorsToAdd { get; set; } = [];
        public List<Domain.Entities.Cellular> CellularToAdd { get; set; } = [];
        public List<Domain.Entities.Cellular> CellularToDelete { get; set; } = [];
    }

    /// <summary>
    /// Container for processing results and statistics
    /// </summary>
    private class ProcessingResults
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int ErrorRecords { get; set; }
        public int CellsCreated { get; set; }
        public int CellSectorsCreated { get; set; }
        public int CellularCreated { get; set; }
        public int NetworksCreated { get; set; }
        public int SectorsCreated { get; set; }
        public int CellularDeleted { get; set; }
        public int SkippedDuplicates { get; set; }
        public int CellSectorsWithoutCellular { get; set; }
        public int RecordsWithoutActiveTechnologies { get; set; }
    }
}
