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
    private readonly IGenerationBandRepository _generationBandRepository;
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository;
    private readonly ITechnologyRepository _technologyRepository;
    private readonly IBandRepository _bandRepository;
    private readonly INetworkRepository _networkRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILinkRepository _linkRepository;
    private readonly IRepository<Domain.Entities.SystemType> _systemTypeRepository;
    private readonly IBlobStorageService _blobStorageService;

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
        IGenerationBandRepository generationBandRepository,
        IRepository<Domain.Entities.CellSector> cellSectorRepository,
        ITechnologyRepository technologyRepository,
        IBandRepository bandRepository,
        INetworkRepository networkRepository,
        IMunicipalityRepository municipalityRepository,
        ILinkRepository linkRepository,
        IRepository<Domain.Entities.SystemType> systemTypeRepository,
        IBlobStorageService blobStorageService
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _cellRepository = cellRepository;
        _cellularRepository = cellularRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _generationBandRepository = generationBandRepository;
        _cellSectorRepository = cellSectorRepository;
        _technologyRepository = technologyRepository;
        _bandRepository = bandRepository;
        _networkRepository = networkRepository;
        _municipalityRepository = municipalityRepository;
        _linkRepository = linkRepository;
        _systemTypeRepository = systemTypeRepository;
        _blobStorageService = blobStorageService;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        // --- Step 1: Initial File Status Update ---
        var fileQuery = _fileRepository.Query(f => f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file == null)
        {
            const string message = "File not found.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        file.Status = FileStatusEnum.Processing;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        try
        {
            // --- Step 2: Download file from blob storage ---
            Stream fileStream;
            try
            {
                if (Uri.TryCreate(parameters.Path, UriKind.Absolute, out var blobUri))
                {
                    // Extract container and blob name from the blob URL
                    var (containerName, blobName) = ExtractBlobInfo(blobUri);
                    
                    _logger.LogInformation("Downloading file from blob storage: container={ContainerName}, blob={BlobName}", 
                        containerName, blobName);
                    
                    fileStream = await _blobStorageService.DownloadAsync(containerName, blobName, token);
                }
                else
                {
                    // Fallback for legacy local file paths
                    _logger.LogWarning("Using legacy file path access for file: {FilePath}", parameters.Path);
                    fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from blob storage or access local file: {FilePath}", parameters.Path);
                await TerminateProcess(file, token);
                return;
            }

            using (fileStream)
            {
                // --- Step 3: Initialize Collections ---
                var cellSectorList = new List<Domain.Entities.CellSector>();
                var cellularToAdd = new List<Domain.Entities.Cellular>();
                var cellularToDelete = new List<Domain.Entities.Cellular>();
                var cellSectorsCreatedForCellId = new Dictionary<Guid, List<Guid>>();
                var networkCache = new Dictionary<string, Guid>();
                var networksToAdd = new List<Domain.Aggregates.Network>();
                var cellsToAdd = new List<Domain.Aggregates.Cell>();

                // --- Step 4: Pre-load Static/Reference Data for Performance ---
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
                    .Query()
                    .Select(t => new
                    {
                        t.Id,
                        Technology = t.Standard
                    })
                    .ToListAsync(token);

                var technologyCache = new Dictionary<string, Guid>();
                foreach (var tech in technologyList)
                {
                    var key = CacheKeyGenerator.GenerateTechnologyCacheKey(tech.Technology);
                    technologyCache[key] = tech.Id;
                }

                _logger.LogInformation("Pre-loaded {Count} technologies: {Technologies}",
                    technologyCache.Count,
                    string.Join(", ", technologyList.Select(t => $"'{t.Technology}' -> '{CacheKeyGenerator.GenerateTechnologyCacheKey(t.Technology)}'")));

                // Log technology diagnostics for debugging
                TechnologyMappingHelper.LogTechnologyDiagnostics(technologyCache, _logger);

                var bandList = await _bandRepository
                    .Query()
                    .Select(b => new
                    {
                        b.Id,
                        Band = b.Name
                    })
                    .ToListAsync(token);

                var bandCache = new Dictionary<string, Guid>();
                foreach (var band in bandList)
                {
                    var key = CacheKeyGenerator.GenerateBandCacheKey(band.Band);
                    bandCache[key] = band.Id;
                }

                _logger.LogInformation("Pre-loaded {Count} bands: {Bands}",
                    bandCache.Count,
                    string.Join(", ", bandList.Select(b => $"'{b.Band}' -> '{CacheKeyGenerator.GenerateBandCacheKey(b.Band)}'")));

                // Pre-load existing networks to avoid duplicates
                var existingNetworks = await _networkRepository
                    .Query(n => serviceProviderCache.Values.Contains(n.ServiceProviderId))
                    .Include(n => n.SystemType)
                    .Select(n => new { n.Id, n.Name, n.ServiceProviderId, n.SystemType.TechnologyId })
                    .ToListAsync(token);

                foreach (var network in existingNetworks)
                {
                    // Try to extract municipality, technology, and band from network name if following our naming convention
                    var nameParts = network.Name.Split('-');
                    if (nameParts.Length >= 3)
                    {
                        var techName = nameParts[0];
                        var bandName = nameParts[1];
                        var municipalityName = nameParts[2];

                        // Look up the actual band ID from the cache
                        var bandCacheKey = CacheKeyGenerator.GenerateBandCacheKey(bandName);
                        if (bandCache.TryGetValue(bandCacheKey, out var bandId))
                        {
                            // Create a cache key with the correct band ID
                            var networkKey = CacheKeyGenerator.GenerateNetworkCacheKey(network.ServiceProviderId, municipalityName, network.TechnologyId, bandId);
                            if (!networkCache.ContainsKey(networkKey))
                            {
                                networkCache[networkKey] = network.Id;
                                _logger.LogDebug("Cached existing network: {NetworkName} with key: {NetworkKey}", network.Name, networkKey);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Could not find band '{BandName}' in cache for existing network '{NetworkName}'. Network will not be cached.", bandName, network.Name);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Network name '{NetworkName}' does not follow expected naming convention 'Technology-Band-Municipality'. Network will not be cached.", network.Name);
                    }
                }
                _logger.LogInformation("Pre-loaded {Count} existing networks.", networkCache.Count);

                var providerIds = serviceProviderCache.Values.ToList();

                // Pre-load municipalities with boundaries for efficient lookup
                var municipalitiesWithBoundaries = await _municipalityRepository
                    .Query(m => m.Boundaries != null)
                    .Select(m => new { m.Id, m.Name, m.Boundaries })
                    .ToListAsync(token);

                // Load existing cells for cache
                var cellList = await _cellRepository
                    .Query(c => providerIds.Contains(c.ServiceProviderId))
                    .ToListAsync(token);

                var cellCache = new Dictionary<string, Guid>();
                var duplicateCount = 0;

                foreach (var cell in cellList)
                {
                    // Normalize TowerType to handle nulls and empty strings consistently
                    var normalizedTowerType = string.IsNullOrWhiteSpace(cell.TowerType) ? "Unknown" : cell.TowerType;
                    var key = CacheKeyGenerator.GenerateCellCacheKey(cell.ServiceProviderId, normalizedTowerType, cell.Location);

                    if (!cellCache.TryGetValue(key, out Guid value))
                    {
                        cellCache[key] = cell.Id;
                    }
                    else
                    {
                        duplicateCount++;
                        _logger.LogWarning("Duplicate cell cache key found: {Key}. Cell ID {CellId} skipped in favor of existing Cell ID {ExistingCellId}. Consider cleaning up duplicate location data.",
                            key, cell.Id, value);
                    }
                }

                _logger.LogInformation("Pre-loaded {Count} existing cells ({DuplicateCount} duplicates handled, {TotalCells} total).",
                    cellCache.Count, duplicateCount, cellList.Count);

                var cellSectorCache = await _cellSectorRepository
                    .Query()
                    .GroupBy(cs => cs.CellId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Select(cs => cs.Id).ToList(),
                        token
                    );
                _logger.LogInformation("Pre-loaded {Count} cell sectors mapped to cells.", cellSectorCache.Count);

                // Pre-load existing Cellular records for duplicate checking
                var cellularList = await _cellularRepository
                    .Query(c => serviceProviderCache.Values.Contains(c.ServiceProviderId))
                    .ToListAsync(token);

                var cellularLookup = new Dictionary<(Guid CellSectorId, Guid ServiceProviderId, Guid TechnologyId, Guid BandId), Domain.Entities.Cellular>();
                var cellularDuplicateCount = 0;

                foreach (var cellular in cellularList)
                {
                    var key = (cellular.CellSectorId, cellular.ServiceProviderId, cellular.TechnologyId, cellular.BandId);

                    if (!cellularLookup.TryGetValue(key, out var existingCellular))
                    {
                        cellularLookup[key] = cellular;
                    }
                    else
                    {
                        cellularDuplicateCount++;
                        _logger.LogWarning("Duplicate cellular record found with key: (CellSectorId: {CellSectorId}, ServiceProviderId: {ServiceProviderId}, TechnologyId: {TechnologyId}, BandId: {BandId}). " +
                            "Cellular ID {CellularId} skipped in favor of existing Cellular ID {ExistingCellularId}. " +
                            "Consider cleaning up duplicate cellular data in the database.",
                            cellular.CellSectorId, cellular.ServiceProviderId, cellular.TechnologyId, cellular.BandId,
                            cellular.Id, existingCellular.Id);
                    }
                }

                _logger.LogInformation("Pre-loaded {Count} existing cellular records ({DuplicateCount} duplicates handled, {TotalCellular} total).",
                    cellularLookup.Count, cellularDuplicateCount, cellularList.Count);

                // --- Step 5: Process the Excel File ---
                _logger.LogInformation("Processing downloaded file from blob storage");

                fileStream.Position = 0;
                _logger.LogInformation("Excel processing started");

                var cellRecords = CellExcelHelper.ParseExcelToCellRecords(fileStream);

                if (cellRecords.Count is 0)
                {
                    _logger.LogInformation("The Excel is empty.");
                    file.Status = FileStatusEnum.Success;
                    _fileRepository.Update(file);
                    await _fileRepository.SaveChangesAsync(token);
                    return;
                }

                var bandTechnologyProperties = typeof(CellRecord).GetProperties()
                    .Where(p => p.PropertyType == typeof(string))
                    .Select(property => new
                    {
                        Property = property,
                        HeaderAttribute = property.GetCustomAttribute<ExcelHeaderAttribute>(),
                        Band = CellPropertyNameParser.GetBandFromPropertyName(property.Name),
                        Technology = CellPropertyNameParser.GetTechnologyFromPropertyName(property.Name)
                    })
                    .Where(item => item.HeaderAttribute != null && !string.IsNullOrEmpty(item.Band) && !string.IsNullOrEmpty(item.Technology))
                    .ToList();

                _logger.LogInformation("Extracted {Count} band/technology properties from Excel: {Properties}",
                    bandTechnologyProperties.Count,
                    string.Join(", ", bandTechnologyProperties.Select(p => $"{p.Property.Name} -> {p.Technology}/{p.Band}")));

                // --- Step 6: Main Loop for Processing Each Cell Record ---
                for (int recordIndex = 0; recordIndex < cellRecords.Count; recordIndex++)
                {
                    var record = cellRecords[recordIndex];
                    var actualRowNumber = recordIndex + 4;

                    ReportProgress($"Processing record from Excel row {actualRowNumber}: {record.Provider}, {record.Zone} {record.Barangay} {record.Municipality} {record.Province} {record.Region}, Coordinates: ({record.Latitude}, {record.Longitude})");
                    _logger.LogInformation("Processing record from Excel row {RowNumber}: {Provider}, {Zone} {Barangay} {Municipality} {Province} {Region}, Coordinates: ({Latitude}, {Longitude})",
                        actualRowNumber, record.Provider, record.Zone, record.Barangay, record.Municipality, record.Province, record.Region, record.Latitude, record.Longitude);

                    var spCacheKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wireless);

                    if (!serviceProviderCache.TryGetValue(spCacheKey, out var serviceProviderId))
                    {
                        _logger.LogWarning("Wireless Service Provider not found in pre-loaded cache: {Provider}. Terminating process.", record.Provider);
                        await TerminateProcess(file, token);
                        return;
                    }

                    Point location = GeometryHelper.CreatePoint(new Coordinate(y: record.Latitude, x: record.Longitude));

                    // TEMPORARY FIX: Swap coordinates if they appear to be in wrong order
                    var actualLatitude = record.Latitude;
                    var actualLongitude = record.Longitude;

                    if (record.Latitude > 50 && record.Longitude < 50)
                    {
                        actualLatitude = record.Longitude;
                        actualLongitude = record.Latitude;
                        _logger.LogWarning("Detected swapped coordinates in Excel row {RowNumber}. " +
                            "Original: Lat={OriginalLat}, Lon={OriginalLon}. " +
                            "Corrected: Lat={CorrectedLat}, Lon={CorrectedLon}",
                            recordIndex + 4, record.Latitude, record.Longitude, actualLatitude, actualLongitude);
                    }

                    // Create location with corrected coordinates
                    location = GeometryHelper.CreatePoint(new Coordinate(y: actualLatitude, x: actualLongitude));

                    var address = $"{record.Zone}, {record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";

                    Guid cellId = Guid.Empty;

                    var cellCacheKey = CacheKeyGenerator.GenerateCellCacheKey(serviceProviderId,
                        string.IsNullOrWhiteSpace(record.TowerType) ? "Unknown" : record.TowerType, location);

                    if (!cellCache.TryGetValue(cellCacheKey, out Guid cachedCellId))
                    {
                        // Cell not in cache, meaning it's a new cell.
                        var cell = new Domain.Aggregates.Cell
                        {
                            Name = $"Cell-{record.Provider}-{address}-{record.TowerType}-{record.Longitude}-{record.Latitude}",
                            Group = record.Provider,
                            Address = address,
                            ServiceProviderId = serviceProviderId,
                            TowerType = string.IsNullOrWhiteSpace(record.TowerType) ? "Unknown" : record.TowerType,
                            Location = location,
                            IsFileUploaded = true,
                        };
                        cellsToAdd.Add(cell);
                        cellId = cell.Id;
                        cellCache[cellCacheKey] = cellId;
                        _logger.LogInformation("Cell {CellId} marked for addition. Name: {Name}", cellId, cell.Name);

                        // Cell Sector Processing (only for newly created cells)
                        if (!String.IsNullOrEmpty(record.Azimuth))
                        {
                            var azimuths = record.Azimuth.Split(",", StringSplitOptions.RemoveEmptyEntries);
                            var beamTilts = record.BeamTilt?.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            if (beamTilts == null || azimuths.Length != beamTilts.Length)
                            {
                                _logger.LogError("The number of Azimuth values ('{AzimuthCount}') does not match the number of BeamTilt values ('{BeamTiltCount}') for record in Excel row {RowNumber}. The file is invalid and needs to be re-uploaded.", azimuths.Length, beamTilts?.Length ?? 0, actualRowNumber);
                                await TerminateProcess(file, token);
                                return;
                            }

                            for (int i = 0; i < azimuths.Length; i++)
                            {
                                var trimmedAzimuth = azimuths[i].Trim();
                                var trimmedBeamTilt = beamTilts[i].Trim();

                                if (decimal.TryParse(trimmedAzimuth, out decimal decimalAzimuth) &&
                                    decimal.TryParse(trimmedBeamTilt, out decimal decimalBeamTilt))
                                {
                                    var cellSector = new Domain.Entities.CellSector
                                    {
                                        CellId = cellId,
                                        AntennaHeight = record.AntennaHeight,
                                        AntennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(record.AntennaType),
                                        Azimuth = decimalAzimuth,
                                        BeamTilt = decimalBeamTilt,
                                    };
                                    cellSectorList.Add(cellSector);

                                    // Track sectors by cell
                                    if (!cellSectorsCreatedForCellId.ContainsKey(cellId))
                                        cellSectorsCreatedForCellId[cellId] = new List<Guid>();

                                    cellSectorsCreatedForCellId[cellId].Add(cellSector.Id);
                                }
                                else
                                {
                                    if (!decimal.TryParse(trimmedAzimuth, out _))
                                    {
                                        _logger.LogWarning("Could not parse '{TrimmedAzimuth}' as a decimal for record in Excel row {RowNumber}, Azimuth index {Index}.", trimmedAzimuth, actualRowNumber, i);
                                    }
                                    if (!decimal.TryParse(trimmedBeamTilt, out _))
                                    {
                                        _logger.LogWarning("Could not parse '{TrimmedBeamTilt}' as a decimal for record in Excel row {RowNumber}, BeamTilt index {Index}.", trimmedBeamTilt, actualRowNumber, i);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Cell found in cache (meaning it already exists in DB)
                        cellId = cachedCellId;
                        _logger.LogInformation("Cell already exists for Service Provider: {Provider}, Location: {location}. Re-using CellId: {CellId}", record.Provider, location, cellId);
                    }

                    // --- Cellular Data Processing Based on File Data Only ---
                    if (cellId != Guid.Empty)
                    {
                        foreach (var propInfo in bandTechnologyProperties)
                        {
                            var contextInfo = $"Excel row {actualRowNumber}";
                            var technologyId = TechnologyMappingHelper.ResolveTechnologyId(propInfo.Technology, technologyCache, _logger, contextInfo);
                            
                            if (!TechnologyMappingHelper.IsTechnologyResolved(technologyId))
                            {
                                _logger.LogError("CRITICAL ERROR: Technology '{Technology}' could not be resolved. " +
                                    "Processing terminated at Excel row {RowNumber}. " +
                                    "Please add the required Technology to the database before processing this file.",
                                    propInfo.Technology, actualRowNumber);
                                await TerminateProcess(file, token);
                                return;
                            }

                            var bandId = TechnologyMappingHelper.ResolveBandId(propInfo.Band, bandCache, _logger, contextInfo);
                            if (!TechnologyMappingHelper.IsBandResolved(bandId))
                            {
                                _logger.LogError("CRITICAL ERROR: Band '{Band}' not found in database. " +
                                    "Processing terminated at Excel row {RowNumber}. " +
                                    "Please add the required Band to the database before processing this file.",
                                    propInfo.Band, actualRowNumber);
                                await TerminateProcess(file, token);
                                return;
                            }

                            // Parse the file data to determine if cellular should be created/deleted
                            var value = propInfo.Property.GetValue(record);
                            var parseResult = ParseBandTechnologyValue(value, propInfo.Technology, propInfo.Band, actualRowNumber);

                            if (cellularLookup.TryGetValue((cellId, serviceProviderId, technologyId, bandId), out var existingCellular))
                            {
                                if (!parseResult.IsActive)
                                {
                                    // File indicates this cellular should not exist - mark for deletion
                                    cellularToDelete.Add(existingCellular);
                                    _logger.LogInformation(
                                        "Marked Cellular record for deletion: CellularId: {CellularId}, CellId: {CellId}, Technology: {Technology}, Band: {Band}",
                                        existingCellular.Id,
                                        cellId,
                                        propInfo.Technology,
                                        propInfo.Band);
                                }
                                else
                                {
                                    // File indicates this cellular should exist - update frequency data if provided
                                    if (parseResult.DLCenterFreq.HasValue)
                                    {
                                        existingCellular.DLCenterFreq = parseResult.DLCenterFreq.Value;
                                        existingCellular.ULCenterFreq = CalculateULCenterFreq(parseResult.DLCenterFreq.Value, propInfo.Technology, propInfo.Band);
                                    }
                                    if (parseResult.Bandwidth.HasValue)
                                    {
                                        existingCellular.Bandwidth = parseResult.Bandwidth.Value;
                                    }

                                    _cellularRepository.Update(existingCellular);
                                    _logger.LogInformation(
                                        "Updated existing Cellular record: CellularId: {CellularId}, Technology: {Technology}, Band: {Band}",
                                        existingCellular.Id, propInfo.Technology, propInfo.Band);
                                }
                            }
                            else
                            {
                                if (parseResult.IsActive)
                                {
                                    // File indicates this cellular should exist but doesn't - create it
                                    // Get all sector ID's for this cell (both existing and newly created)
                                    var allSectorIds = new List<Guid>();

                                    // Add existing sectors from cache
                                    if (cellSectorCache.TryGetValue(cellId, out var existingSectorIds))
                                    {
                                        allSectorIds.AddRange(existingSectorIds);
                                    }

                                    // Add newly created sectors for this cell
                                    if (cellSectorsCreatedForCellId.TryGetValue(cellId, out var newSectorIds))
                                    {
                                        allSectorIds.AddRange(newSectorIds);
                                    }

                                    if (allSectorIds.Count > 0)
                                    {
                                        _logger.LogInformation("Found {SectorCount} total sector(s) for CellId: {CellId} ({ExistingCount} existing, {NewCount} new)",
                                            allSectorIds.Count, cellId, existingSectorIds?.Count ?? 0, newSectorIds?.Count ?? 0);

                                        foreach (var sectorId in allSectorIds)
                                        {
                                            var key = (sectorId, serviceProviderId, technologyId, bandId);

                                            // Check if cellular entity already exists
                                            if (!cellularLookup.ContainsKey(key))
                                            {
                                                // Double-check database to ensure we don't create duplicates
                                                var existsInDatabase = await _cellularRepository
                                                    .Query(c => c.CellSectorId == sectorId &&
                                                               c.ServiceProviderId == serviceProviderId &&
                                                               c.TechnologyId == technologyId &&
                                                               c.BandId == bandId)
                                                    .AnyAsync(token);

                                                if (!existsInDatabase)
                                                {
                                                    // Apply values from file or defaults
                                                    var dlCenterFreq = parseResult.DLCenterFreq ?? GetDefaultDLCenterFreq(propInfo.Technology, propInfo.Band);
                                                    var bandwidth = parseResult.Bandwidth ?? GetDefaultBandwidth(propInfo.Technology, propInfo.Band);
                                                    var ulCenterFreq = CalculateULCenterFreq(dlCenterFreq, propInfo.Technology, propInfo.Band);

                                                    // Get municipality for this cell's location
                                                    var (municipalityId, municipalityName) = GetMunicipalityByLocationOptimized(location, municipalitiesWithBoundaries);

                                                    // Get or create network for this combination
                                                    var networkId = await GetOrCreateNetworkAsync(
                                                        serviceProviderId,
                                                        municipalityName,
                                                        technologyId,
                                                        bandId,
                                                        networkCache,
                                                        networksToAdd,
                                                        technologyCache,
                                                        bandCache,
                                                        dlCenterFreq,
                                                        ulCenterFreq,
                                                        bandwidth,
                                                        token);

                                                    // Only create cellular if we have a valid network
                                                    if (networkId == Guid.Empty)
                                                    {
                                                        _logger.LogError(
                                                            "CRITICAL ERROR: Cannot create Cellular record for CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band} " +
                                                            "because required network dependencies (SystemType, Links) are missing for ServiceProvider: {ServiceProviderId}. " +
                                                            "Processing terminated at Excel row {RowNumber}. Please ensure all required dependencies exist in database.",
                                                            sectorId, propInfo.Technology, propInfo.Band, serviceProviderId, actualRowNumber);
                                                        await TerminateProcess(file, token);
                                                        return;
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

                                                    cellularToAdd.Add(newCellular);
                                                    cellularLookup[key] = newCellular;

                                                    _logger.LogInformation(
                                                        "Marked Cellular record for addition: CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band}, NetworkId: {NetworkId}, Municipality: {Municipality}, DLCenterFreq: {DLCenterFreq}, ULCenterFreq: {ULCenterFreq}, Bandwidth: {Bandwidth}",
                                                        sectorId, propInfo.Technology, propInfo.Band, networkId, municipalityName, newCellular.DLCenterFreq, newCellular.ULCenterFreq, newCellular.Bandwidth);
                                                }
                                                else
                                                {
                                                    _logger.LogInformation(
                                                        "Skipped Cellular record creation (already exists in database): CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band}",
                                                        sectorId, propInfo.Technology, propInfo.Band);
                                                }
                                            }
                                            else
                                            {
                                                _logger.LogInformation(
                                                    "Skipped existing Cellular record: CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band}",
                                                    sectorId, propInfo.Technology, propInfo.Band);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No cell sectors found for cell {CellId}. Skipping cellular creation for this cell.", cellId);
                                    }
                                }
                                // If parseResult.IsActive is false, do nothing - cellular doesn't exist and file doesn't want it
                            }
                        }
                    }
                }

                // --- Step 7: Batch Save All Accumulated Changes ---
                // Batch add new networks (before cellular records since they depend on networks)
                if (networksToAdd.Count != 0)
                {
                    await _networkRepository.BulkInsertAsync(networksToAdd, token);
                    _logger.LogInformation("{Count} new networks saved successfully (batched).", networksToAdd.Count);
                }
                else
                {
                    _logger.LogInformation("No new networks to add (batched).");
                }

                // Batch add new cells
                if (cellsToAdd.Count != 0)
                {
                    await _cellRepository.BulkInsertAsync(cellsToAdd, token);
                    _logger.LogInformation("{Count} new cells saved successfully (batched).", cellsToAdd.Count);
                }
                else
                {
                    _logger.LogInformation("No new cells to add (batched).");
                }

                // Batch add new cell sectors
                if (cellSectorList.Count != 0)
                {
                    await _cellSectorRepository.BulkInsertAsync(cellSectorList, token);
                    _logger.LogInformation("{Count} sector records saved successfully (batched).", cellSectorList.Count);

                    // Update cellSectorCache with newly created sectors
                    foreach (var sector in cellSectorList)
                    {
                        if (!cellSectorCache.ContainsKey(sector.CellId))
                            cellSectorCache[sector.CellId] = new List<Guid>();
                        cellSectorCache[sector.CellId].Add(sector.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("No sector records to add.");
                }

                // Batch add new cellular entries
                if (cellularToAdd.Count != 0)
                {
                    await _cellularRepository.BulkInsertAsync(cellularToAdd, token);
                    _logger.LogInformation("{Count} cellular records saved successfully (batched).", cellularToAdd.Count);
                }
                else
                {
                    _logger.LogInformation("No cellular records to add.");
                }

                // Batch delete cellular entries
                if (cellularToDelete.Count != 0)
                {
                    await _cellularRepository.BulkDeleteAsync(cellularToDelete);
                    _logger.LogInformation("{Count} cellular records deleted successfully (batched).", cellularToDelete.Count);
                }

                // --- Step 8: Final File Status Update ---
                file.Status = FileStatusEnum.Success;
                _fileRepository.Update(file);
                await _fileRepository.SaveChangesAsync(token);

                _logger.LogInformation("Processing completed successfully. " +
                    "If you encounter termination errors in future runs, ensure the following dependencies exist in the database:");
                _logger.LogInformation("1. Technologies: Required for all technology/band combinations in your Excel file");
                _logger.LogInformation("2. Bands: Required for all technology/band combinations in your Excel file");
                _logger.LogInformation("3. SystemTypes: Required for each Technology to create Networks");
                _logger.LogInformation("4. Links (Uplink/Downlink): Required for each ServiceProvider/Band combination to create Networks");
                _logger.LogInformation("Current database contains - Technologies: {TechCount}, Bands: {BandCount}, Networks: {NetworkCount}",
                    technologyCache.Count, bandCache.Count, networkCache.Count);

                _logger.LogInformation("Processing completed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing file.");
            await TerminateProcess(file, token);
            throw;
        }
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

        return (containerName, blobName);
    }

    /// <summary>
    /// Gets default DL center frequency based on technology and band when not specified in Excel
    /// </summary>
    private static decimal GetDefaultDLCenterFreq(string technology, string band)
    {
        // Return technology and band specific default frequencies or fallback to 0
        return (technology.ToUpperInvariant(), band) switch
        {
            ("GSM", "Band 8") => 947.5m,     // 900 MHz GSM center
            ("GSM", "Band 3") => 1842.5m,    // 1800 MHz GSM center
            ("EDGE", "Band 8") => 947.5m,    // 900 MHz EDGE (GSM-based)
            ("EDGE", "Band 3") => 1842.5m,   // 1800 MHz EDGE (GSM-based)
            ("WCDMA", "Band 5") => 869m,     // 850 MHz WCDMA center
            ("WCDMA", "Band 8") => 950m,     // 900 MHz WCDMA center
            ("WCDMA", "Band 1") => 2140m,    // 2100 MHz WCDMA center
            ("HSPAP", "Band 5") => 869m,     // 850 MHz HSPAP (WCDMA-based)
            ("HSPAP", "Band 8") => 950m,     // 900 MHz HSPAP (WCDMA-based)
            ("HSPAP", "Band 1") => 2140m,    // 2100 MHz HSPAP (WCDMA-based)
            ("FDDLTE", "Band 28") => 758m,  // 700 MHz LTE center
            ("FDDLTE", "Band 5") => 869m,   // 850 MHz LTE center
            ("FDDLTE", "Band 8") => 950m,   // 900 MHz LTE center
            ("FDDLTE", "Band 3") => 1842.5m, // 1800 MHz LTE center
            ("FDDLTE", "Band 1") => 2140m,  // 2100 MHz LTE center
            ("LTE", "Band 28") => 758m,      // 700 MHz LTE center (generic LTE)
            ("LTE", "Band 5") => 869m,       // 850 MHz LTE center (generic LTE)
            ("LTE", "Band 8") => 950m,       // 900 MHz LTE center (generic LTE)
            ("LTE", "Band 3") => 1842.5m,    // 1800 MHz LTE center (generic LTE)
            ("LTE", "Band 1") => 2140m,      // 2100 MHz LTE center (generic LTE)
            ("TDDLTE", "Band 40") => 2300m, // 2300 MHz TDD center
            ("TDDLTE", "Band 41") => 2500m, // 2500 MHz TDD center
            ("TDDLTE", "Band 38") => 2600m, // 2600 MHz TDD center
            ("NR", "Band 78") => 3500m,      // 3500 MHz NR center
            ("NR", "Band 38") => 2600m,      // 2600 MHz NR center
            // Handle flexible TDD/FDD matching
            var (tech, bandName) when tech.Contains("TDD") => GetTDDDefaultFreq(bandName),
            var (tech, bandName) when tech.Contains("FDD") => GetFDDDefaultFreq(bandName),
            _ => DEFAULT_DL_CENTER_FREQ       // Fallback to 0
        };
    }

    /// <summary>
    /// Gets default frequency for any TDD technology based on band
    /// </summary>
    private static decimal GetTDDDefaultFreq(string band)
    {
        return band switch
        {
            "Band 40" => 2300m, // 2300 MHz TDD center
            "Band 41" => 2500m, // 2500 MHz TDD center
            "Band 38" => 2600m, // 2600 MHz TDD center
            "Band 42" => 3400m, // 3400 MHz TDD center
            "Band 78" => 3500m, // 3500 MHz TDD center
            _ => DEFAULT_DL_CENTER_FREQ
        };
    }

    /// <summary>
    /// Gets default frequency for any FDD technology based on band
    /// </summary>
    private static decimal GetFDDDefaultFreq(string band)
    {
        return band switch
        {
            "Band 28" => 758m,   // 700 MHz FDD center
            "Band 5" => 869m,    // 850 MHz FDD center
            "Band 8" => 950m,    // 900 MHz FDD center
            "Band 3" => 1842.5m, // 1800 MHz FDD center
            "Band 1" => 2140m,   // 2100 MHz FDD center
            _ => DEFAULT_DL_CENTER_FREQ
        };
    }
    /// <summary>
    /// Gets default bandwidth based on technology and band when not specified in Excel
    /// </summary>
    private static decimal GetDefaultBandwidth(string technology, string band)
    {
        // Return technology-specific default bandwidths
        return technology.ToUpperInvariant() switch
        {
            "GSM" => 0.2m,       // 200 kHz
            "EDGE" => 0.2m,      // 200 kHz (GSM-based)
            "WCDMA" => 5m,       // 5 MHz
            "HSPAP" => 5m,       // 5 MHz (WCDMA-based)
            "FDDLTE" => 20m,    // 20 MHz
            "TDDLTE" => 20m,    // 20 MHz
            "LTE" => 20m,        // 20 MHz (generic LTE)
            "NR" => 100m,        // 100 MHz for 5G
            // Handle flexible TDD/FDD matching
            var tech when tech.Contains("TDD") => 20m,  // Default TDD bandwidth
            var tech when tech.Contains("FDD") => 20m,  // Default FDD bandwidth
            _ => DEFAULT_BANDWIDTH // 20 MHz default
        };
    }

    /// <summary>
    /// Calculates the frequency range for a link based on center frequency and bandwidth
    /// </summary>
    private static (decimal LowerFreq, decimal UpperFreq) CalculateLinkFrequencyRange(decimal centerFreq, decimal bandwidth)
    {
        var halfBandwidth = bandwidth / 2;
        return (centerFreq - halfBandwidth, centerFreq + halfBandwidth);
    }

    /// <summary>
    /// Gets default RxThreshold based on technology type
    /// </summary>
    private static decimal GetDefaultRxThreshold(string technology)
    {
        return technology.ToUpperInvariant() switch
        {
            "GSM" => -110m,      // GSM typically has lower sensitivity
            "EDGE" => -110m,     // EDGE similar to GSM
            "WCDMA" => -105m,    // WCDMA better sensitivity
            "HSPAP" => -105m,    // HSPAP similar to WCDMA
            "FDDLTE" => -100m,  // LTE good sensitivity
            "TDDLTE" => -100m,  // TDD-LTE similar to FDDLTE
            "LTE" => -100m,      // Generic LTE
            "NR" => -95m,        // 5G NR best sensitivity
            _ => -100m           // Default fallback
        };
    }

    void ReportProgress(string message)
    {
        _logger.LogInformation(message);
    }

    private async Task TerminateProcess(Domain.Entities.File file, CancellationToken cancellationToken)
    {
        file.Status = FileStatusEnum.Failed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogError("=== PROCESSING TERMINATED DUE TO MISSING DEPENDENCIES ===");
        _logger.LogError("File processing has been terminated because required database entities are missing.");
        _logger.LogError("Please ensure the following entities exist in your database before re-uploading the file:");
        _logger.LogError("1. SERVICE PROVIDERS: All providers mentioned in your Excel file must exist as 'Wireless' service providers");
        _logger.LogError("2. TECHNOLOGIES: All technologies (GSM, WCDMA, LTE-FDD, LTE-TDD, NR, etc.) must exist in the Technologies table");
        _logger.LogError("3. BANDS: All bands (Band 28, Band 5, Band 8, Band 3, etc.) must exist in the Bands table");
        _logger.LogError("4. SYSTEM TYPES: Each technology must have a corresponding SystemType entry");
        _logger.LogError("5. LINKS: Each ServiceProvider + Band combination must have both Uplink and Downlink entries");
        _logger.LogError("Contact your system administrator to add the missing entities before re-processing this file.");
        _logger.LogError("======================================================");
    }

    /// <summary>
    /// Parses the band/technology column value which can be:
    /// - Empty/null (defaults to false)
    /// - "TRUE"/"FALSE" (boolean values)
    /// - "CF: {decimal}, BW: {decimal}" format (active with frequency data)
    /// </summary>
    private BandTechnologyParseResult ParseBandTechnologyValue(object? value, string technology, string band, int rowNumber)
    {
        var result = new BandTechnologyParseResult();

        if (value == null)
        {
            result.IsActive = false;
            return result;
        }

        var stringValue = value.ToString()?.Trim() ?? string.Empty;

        // Handle empty values (default to false)
        if (string.IsNullOrEmpty(stringValue))
        {
            result.IsActive = false;
            return result;
        }

        // Handle boolean values
        if (stringValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || stringValue == "1")
        {
            result.IsActive = true;
            // When TRUE but no frequency data, we'll use defaults in the calling code
            return result;
        }

        if (stringValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase) || stringValue == "0")
        {
            result.IsActive = false;
            return result;
        }

        // Handle frequency format: CF: {decimal}, BW: {decimal} (without square brackets)
        var pattern = @"CF:\s*([0-9]*\.?[0-9]+),\s*BW:\s*([0-9]*\.?[0-9]+)";
        var match = Regex.Match(stringValue, pattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            result.IsActive = true;
            
            if (decimal.TryParse(match.Groups[1].Value, out var dlCenterFreq))
            {
                result.DLCenterFreq = dlCenterFreq;
            }
            else
            {
                _logger.LogWarning("Could not parse DL Center Frequency '{DLFreq}' for {Technology} {Band} in row {RowNumber}", 
                    match.Groups[1].Value, technology, band, rowNumber);
            }

            if (decimal.TryParse(match.Groups[2].Value, out var bandwidth))
            {
                result.Bandwidth = bandwidth;
            }
            else
            {
                _logger.LogWarning("Could not parse Bandwidth '{Bandwidth}' for {Technology} {Band} in row {RowNumber}", 
                    match.Groups[2].Value, technology, band, rowNumber);
            }

            return result;
        }

        // If none of the above patterns match, log a warning and default to false
        _logger.LogWarning("Could not parse band/technology value '{Value}' for {Technology} {Band} in row {RowNumber}. Defaulting to false.", 
            stringValue, technology, band, rowNumber);
        
        result.IsActive = false;
        return result;
    }

    /// <summary>
    /// Calculates the UL (Uplink) center frequency based on DL (Downlink) center frequency,
    /// technology type, and band specifications.
    /// </summary>
    private decimal CalculateULCenterFreq(decimal dlCenterFreq, string technology, string band)
    {
        // For TDD technologies (like TDD-LTE, NR-TDD), UL and DL use the same frequency
        if (technology.Contains("TDD", StringComparison.OrdinalIgnoreCase) || 
            (technology.Equals("NR", StringComparison.OrdinalIgnoreCase) && IsTDDBand(band)))
        {
            return dlCenterFreq;
        }

        // For FDD technologies, calculate based on band-specific duplex spacing
        return technology.ToUpperInvariant() switch
        {
            "GSM" => CalculateGsmULFreq(dlCenterFreq, band),
            "EDGE" => CalculateGsmULFreq(dlCenterFreq, band), // EDGE uses GSM frequency plan
            "WCDMA" => CalculateWcdmaULFreq(dlCenterFreq, band),
            "HSPAP" => CalculateWcdmaULFreq(dlCenterFreq, band), // HSPAP uses WCDMA frequency plan
            "FDDLTE" => CalculateLteFddULFreq(dlCenterFreq, band),
            "LTE" => CalculateLteFddULFreq(dlCenterFreq, band), // Default LTE to FDD calculation
            "TDDLTE" => dlCenterFreq, // TDD uses same frequency for UL and DL
            "NR" => CalculateNrFddULFreq(dlCenterFreq, band),
            _ => dlCenterFreq // Default to DL frequency if technology is unknown
        };
    }

    /// <summary>
    /// Determines if a band is TDD-based for NR technology
    /// </summary>
    private static bool IsTDDBand(string band)
    {
        // Common TDD bands for NR
        var tddBands = new[] { "Band 38", "Band 40", "Band 41", "Band 42", "Band 78" };
        return tddBands.Contains(band, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates GSM UL frequency based on DL frequency and band
    /// </summary>
    private static decimal CalculateGsmULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 8" => dlCenterFreq - 45, // 900 MHz band (DL: 935-960, UL: 890-915)
            "Band 3" => dlCenterFreq - 95, // 1800 MHz band (DL: 1805-1880, UL: 1710-1785)
            _ => dlCenterFreq - 45 // Default GSM duplex spacing
        };
    }

    /// <summary>
    /// Calculates WCDMA UL frequency based on DL frequency and band
    /// </summary>
    private static decimal CalculateWcdmaULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 5" => dlCenterFreq - 45,  // 850 MHz band
            "Band 8" => dlCenterFreq - 45,  // 900 MHz band
            "Band 1" => dlCenterFreq - 190, // 2100 MHz band
            _ => dlCenterFreq - 190 // Default WCDMA duplex spacing
        };
    }

    /// <summary>
    /// Calculates LTE FDD UL frequency based on DL frequency and band
    /// </summary>
    private static decimal CalculateLteFddULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 28" => dlCenterFreq - 55,  // 700 MHz band
            "Band 5" => dlCenterFreq - 45,   // 850 MHz band
            "Band 8" => dlCenterFreq - 45,   // 900 MHz band
            "Band 3" => dlCenterFreq - 95,   // 1800 MHz band
            "Band 1" => dlCenterFreq - 190,  // 2100 MHz band
            _ => dlCenterFreq - 45 // Default LTE FDD duplex spacing
        };
    }

    /// <summary>
    /// Calculates NR FDD UL frequency based on DL frequency and band
    /// </summary>
    private static decimal CalculateNrFddULFreq(decimal dlCenterFreq, string band)
    {
        return band switch
        {
            "Band 28" => dlCenterFreq - 55,  // 700 MHz band
            "Band 1" => dlCenterFreq - 190,  // 2100 MHz band
            "Band 3" => dlCenterFreq - 95,   // 1800 MHz band
            _ => dlCenterFreq - 45 // Default NR FDD duplex spacing
        };
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
    /// Gets municipality by location using pre-loaded municipality data for better performance
    /// </summary>
    private (Guid? Id, string Name) GetMunicipalityByLocationOptimized(
        Point location, 
        IEnumerable<dynamic> municipalitiesWithBoundaries)
    {
        try
        {
            if (location?.IsValid != true)
            {
                return (null, "Unknown");
            }

            var municipality = municipalitiesWithBoundaries
                .FirstOrDefault(m => m.Boundaries?.Intersects(location) == true);

            if (municipality != null)
            {
                _logger.LogDebug("Found municipality: {MunicipalityName} for location: {Location}", 
                    (string)municipality.Name, location);
                return ((Guid)municipality.Id, (string)municipality.Name);
            }

            _logger.LogWarning("No municipality found for location: {Location}", location);
            return (null, "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up municipality for location: {Location}", location);
            return (null, "Unknown");
        }
    }

    /// <summary>
    /// Gets or creates a Network for the given combination of ServiceProvider + Municipality + Technology + Band
    /// </summary>
    private async Task<Guid> GetOrCreateNetworkAsync(
        Guid serviceProviderId, 
        string municipalityName,
        Guid technologyId, 
        Guid bandId,
        Dictionary<string, Guid> networkCache,
        List<Domain.Aggregates.Network> networksToAdd,
        Dictionary<string, Guid> technologyCache,
        Dictionary<string, Guid> bandCache,
        decimal dlCenterFreq,
        decimal ulCenterFreq,
        decimal bandwidth,
        CancellationToken token)
    {
        var networkCacheKey = CacheKeyGenerator.GenerateNetworkCacheKey(serviceProviderId, municipalityName, technologyId, bandId);
        
        // Check if network already exists in cache
        if (networkCache.TryGetValue(networkCacheKey, out var existingNetworkId))
        {
            return existingNetworkId;
        }

        // Create technology and band name strings for queries and naming
        var technologyName = technologyCache.FirstOrDefault(x => x.Value == technologyId).Key ?? "Unknown";
        var bandName = bandCache.FirstOrDefault(x => x.Value == bandId).Key ?? "Unknown";

        // Check if network already exists in database
        var networkNamePattern = $"{technologyName}-{bandName}-{municipalityName}";
        var existingNetwork = await _networkRepository
            .Query(n => n.ServiceProviderId == serviceProviderId && 
                       n.Name.StartsWith(networkNamePattern))
            .FirstOrDefaultAsync(token);

        if (existingNetwork != null)
        {
            networkCache[networkCacheKey] = existingNetwork.Id;
            return existingNetwork.Id;
        }

        // Lookup existing SystemType for this technology - DO NOT CREATE if missing
        var systemType = await _systemTypeRepository
            .Query(st => st.TechnologyId == technologyId)
            .FirstOrDefaultAsync(token);

        if (systemType == null)
        {
            // Look up the technology to get its generation for proper error reporting
            var technology = await _technologyRepository
                .Query(t => t.Id == technologyId)
                .Include(t => t.Generation)
                .FirstOrDefaultAsync(token);

            var technologyInfo = technology != null ? $"{technology.Standard} (Generation: {technology.Generation?.Name})" : "Unknown";

            _logger.LogError(
                "CRITICAL ERROR: SystemType not found for Technology: {TechnologyInfo}. " +
                "Cannot create network without existing SystemType. " +
                "Processing terminated. Please ensure SystemType exists in database for " +
                "ServiceProvider: {ServiceProviderId}, Municipality: {MunicipalityName}, Technology: {Technology}, Band: {Band}",
                technologyInfo, serviceProviderId, municipalityName, technologyName, bandName);

            return Guid.Empty;
        }

        // Create network with calculated frequencies and bandwidth directly
        var network = new Domain.Aggregates.Network
        {
            Name = $"{technologyName}-{bandName}-{municipalityName}",
            ServiceProviderId = serviceProviderId,
            SystemTypeId = systemType.Id,
            ULCenterFreq = ulCenterFreq,
            DLCenterFreq = dlCenterFreq,
            Bandwidth = bandwidth
        };

        networksToAdd.Add(network);
        networkCache[networkCacheKey] = network.Id;

        _logger.LogInformation(
            "Created new Network: {NetworkName} for ServiceProvider: {ServiceProviderId}, Municipality: {MunicipalityName}, Technology: {Technology}, Band: {Band}, " +
            "ULCenterFreq: {ULCenterFreq} MHz, DLCenterFreq: {DLCenterFreq} MHz, Bandwidth: {Bandwidth} MHz",
            network.Name, serviceProviderId, municipalityName, technologyName, bandName, ulCenterFreq, dlCenterFreq, bandwidth);

        return network.Id;
    }
}
