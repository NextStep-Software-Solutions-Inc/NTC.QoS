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
        IRepository<Domain.Entities.SystemType> systemTypeRepository
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
    }

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

        // --- Step 2 ---
        var cellSectorList = new List<Domain.Entities.CellSector>();
        var cellularToAdd = new List<Domain.Entities.Cellular>();
        var cellularToDelete = new List<Domain.Entities.Cellular>();
        var cellSectorsCreatedForCellId = new Dictionary<Guid, List<Guid>>();
        var networkCache = new Dictionary<string, Guid>();
        var networksToAdd = new List<Domain.Aggregates.Network>();

        // IMPORTANT: Declare cellsToAdd list here, BEFORE the main loop,
        // so it can accumulate new cells across all records.
        var cellsToAdd = new List<Domain.Aggregates.Cell>();

        // --- Step 3: Pre-load Static/Reference Data for Performance ---
        _logger.LogInformation("Pre-loading reference data.");

        // Pre-load all wireless service providers
        var serviceProviderCache = await _serviceProviderRepository
            .Query(sp => sp.Service.Type == ServiceTypeEnum.Wireless)
            .Select(sp => new { sp.Id, sp.Provider.Name })
            .ToDictionaryAsync( // 1. Fetches data AND populates the dictionary in one go
                 sp => CacheKeyGenerator.GenerateServiceProviderCacheKey(sp.Name, ServiceTypeEnum.Wireless), 
                 sp => sp.Id,                                          
                 token
            );
        _logger.LogInformation("Pre-loaded {Count} wireless service providers.", serviceProviderCache.Count);

        // Pre-load technologies with additional logging
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
                
                // Create a cache key based on naming convention
                var networkKey = CacheKeyGenerator.GenerateNetworkCacheKey(network.ServiceProviderId, municipalityName, network.TechnologyId, Guid.Empty);
                if (!networkCache.ContainsKey(networkKey))
                {
                    networkCache[networkKey] = network.Id;
                }
            }
        }

        _logger.LogInformation("Pre-loaded {Count} existing networks.", networkCache.Count);

        // Pre-load existing links to optimize network creation
        var linkCache = new Dictionary<string, Guid>();
        var providerIds = serviceProviderCache.Values.ToList();
        var existingLinks = await _linkRepository
            .Query(l => providerIds.Contains(l.ServiceProviderId))
            .Select(l => new { l.Id, l.ServiceProviderId, l.BandId, l.Type })
            .ToListAsync(token);

        foreach (var link in existingLinks)
        {
            var linkKey = $"{link.ServiceProviderId}_{link.BandId}_{link.Type}";
            if (!linkCache.ContainsKey(linkKey))
            {
                linkCache[linkKey] = link.Id;
            }
        }

        _logger.LogInformation("Pre-loaded {Count} existing links.", linkCache.Count);

        // Fix: Handle duplicate cell cache keys gracefully by loading cells first, then building dictionary manually
        var cellList = await _cellRepository
            .Query(c => providerIds.Contains(c.ServiceProviderId))
            .ToListAsync(token);

        // Build cache manually to handle duplicates gracefully
        var cellCache = new Dictionary<string, Guid>();
        var duplicateCount = 0;

        foreach (var cell in cellList)
        {
            // Normalize TowerType to handle nulls and empty strings consistently
            var normalizedTowerType = string.IsNullOrWhiteSpace(cell.TowerType) ? "Unknown" : cell.TowerType;
            var key = CacheKeyGenerator.GenerateCellCacheKey(cell.ServiceProviderId, normalizedTowerType, cell.Location);
            
            if (!cellCache.ContainsKey(key))
            {
                cellCache[key] = cell.Id;
            }
            else
            {
                duplicateCount++;
                _logger.LogWarning("Duplicate cell cache key found: {Key}. Cell ID {CellId} skipped in favor of existing Cell ID {ExistingCellId}. Consider cleaning up duplicate location data.",
                    key, cell.Id, cellCache[key]);
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

        // Pre-load existing Cellular records for efficient in-memory checks.
        var cellularLookup = await _cellularRepository
            .Query(c => serviceProviderCache.Values.Contains(c.ServiceProviderId))
            .ToDictionaryAsync(
                c => (c.CellSectorId, c.ServiceProviderId, c.TechnologyId, c.BandId),
                c => c,
                token
            );
        _logger.LogInformation("Pre-loaded {Count} existing cellular records.", cellularLookup.Count);


        // --- Step 4: Process the Excel File ---
        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        _logger.LogInformation("Processing file: {FilePath}", parameters.Path);

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
            .Where(p => p.PropertyType == typeof(string)) // Changed to string since we updated CellRecord
            .Select(property => new
            {
                Property = property,
                HeaderAttribute = property.GetCustomAttribute<ExcelHeaderAttribute>(),
                Band = GetBandFromPropertyName(property.Name),
                Technology = GetTechnologyFromPropertyName(property.Name)
            })
            .Where(item => item.HeaderAttribute != null && !string.IsNullOrEmpty(item.Band) && !string.IsNullOrEmpty(item.Technology))
            .ToList();

        // Log the extracted band/technology mappings for debugging
        _logger.LogInformation("Extracted {Count} band/technology properties from Excel: {Properties}", 
            bandTechnologyProperties.Count,
            string.Join(", ", bandTechnologyProperties.Select(p => $"{p.Property.Name} -> {p.Technology}/{p.Band}")));
        // --- Step 5: Main Loop for Processing Each Cell Record ---
        for (int recordIndex = 0; recordIndex < cellRecords.Count; recordIndex++)
        {
            var record = cellRecords[recordIndex];
            var actualRowNumber = recordIndex + 4;

            ReportProgress($"Processing record from Excel row {actualRowNumber}: {record.Provider}, {record.Zone} {record.Barangay} {record.Municipality} {record.Province} {record.Region}, Coordinates: ({record.Latitude}, {record.Longitude})");
            _logger.LogInformation("Processing record from Excel row {RowNumber}: {Provider}, {Zone} {Barangay} {Municipality} {Province} {Region}, Coordinates: ({Latitude}, {Longitude})",
                actualRowNumber, record.Provider, record.Zone, record.Barangay, record.Municipality, record.Province, record.Region, record.Latitude, record.Longitude);

            // Inside the main loop
            // Ensure the entire key is lowercased to match what was stored
            var spCacheKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wireless);

            if (!serviceProviderCache.TryGetValue(spCacheKey, out var serviceProviderId))
            {
                // This log now genuinely means it's not found in the DB (or is not wireless)
                _logger.LogWarning("Wireless Service Provider not found in pre-loaded cache: {Provider}. Terminating process.", record.Provider);
                await TerminateProcess(file, token);
                return;
            }

            Point location = GeometryHelper.CreatePoint(new Coordinate(y: record.Latitude, x: record.Longitude));
            var address = $"{record.Zone}, {record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";

            Guid cellId = Guid.Empty;
            // The check for serviceProviderId != Guid.Empty is already handled by the `TryGetValue` above.
            // If we reach here, serviceProviderId is valid.

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
                cellsToAdd.Add(cell); // Add to the list for batch save
                cellId = cell.Id; // Get the ID of the new cell
                cellCache[cellCacheKey] = cellId; // Add to cache for subsequent lookups in this file
                _logger.LogInformation("Cell {CellId} marked for addition. Name: {Name}", cellId, cell.Name);

                // Cell Sector Processing (only for newly created cells, as per original logic)
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

                            cellSectorsCreatedForCellId[cellId].Add(cellSector.Id); // Save the sector Id for later use
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

            // --- Cellular Data Processing ---
            if (cellId != Guid.Empty // Should always be true if we got cellId
            ){
                foreach (var propInfo in bandTechnologyProperties)
                {
                    var technologyCacheKey = CacheKeyGenerator.GenerateTechnologyCacheKey(propInfo.Technology);
                    if (!technologyCache.TryGetValue(technologyCacheKey, out var technologyId))
                    {
                        // Handle specific technology mappings for missing technologies
                        string? alternativeTechnology = propInfo.Technology.ToUpperInvariant() switch
                        {
                            "GSM" => "EDGE", // Map GSM to EDGE if GSM is not available
                            "WCDMA" => "HSPAP", // Map WCDMA to HSPAP if WCDMA is not available  
                            _ => null
                        };

                        if (alternativeTechnology != null)
                        {
                            var alternativeCacheKey = CacheKeyGenerator.GenerateTechnologyCacheKey(alternativeTechnology);
                            if (technologyCache.TryGetValue(alternativeCacheKey, out technologyId))
                            {
                                _logger.LogInformation("Technology '{Technology}' not found, using alternative '{Alternative}' for Excel row {RowNumber}", 
                                    propInfo.Technology, alternativeTechnology, actualRowNumber);
                            }
                            else
                            {
                                _logger.LogWarning("Technology '{Technology}' and alternative '{Alternative}' not found. Skipping this technology/band combination for Excel row {RowNumber}. Available technologies: {AvailableTechnologies}", 
                                    propInfo.Technology, alternativeTechnology, actualRowNumber, string.Join(", ", technologyCache.Keys));
                                continue; // Skip this technology instead of terminating the entire process
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Technology '{Technology}' not found and no alternative mapping available. Skipping this technology/band combination for Excel row {RowNumber}. Available technologies: {AvailableTechnologies}", 
                                propInfo.Technology, actualRowNumber, string.Join(", ", technologyCache.Keys));
                            continue; // Skip this technology instead of terminating the entire process
                        }
                    }

                    var bandCacheKey = CacheKeyGenerator.GenerateBandCacheKey(propInfo.Band);
                    if (!bandCache.TryGetValue(bandCacheKey, out var bandId))
                    {
                        _logger.LogWarning("Band '{Band}' not found in pre-loaded cache. Skipping this technology/band combination for Excel row {RowNumber}. Available bands: {AvailableBands}", 
                            propInfo.Band, actualRowNumber, string.Join(", ", bandCache.Keys));
                        continue; // Skip this band instead of terminating the entire process
                    }

                    // Parse the new column format
                    var value = propInfo.Property.GetValue(record);
                    var parseResult = ParseBandTechnologyValue(value, propInfo.Technology, propInfo.Band, actualRowNumber);
                    
                    if (cellularLookup.TryGetValue((cellId, serviceProviderId, technologyId, bandId), out var existingCellular))
                    {
                        if (!parseResult.IsActive)
                        {
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
                            // Update existing cellular with new frequency data if provided
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
                        }
                    }
                    else
                    {
                        if (parseResult.IsActive)
                        {
                            // Get all sector IDs for this cell (both existing and newly created)
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
                                    if (!cellularLookup.ContainsKey(key))
                                    {
                                        // Apply default values when TRUE is set but no frequency data provided
                                        var dlCenterFreq = parseResult.DLCenterFreq ?? GetDefaultDLCenterFreq(propInfo.Technology, propInfo.Band);
                                        var bandwidth = parseResult.Bandwidth ?? GetDefaultBandwidth(propInfo.Technology, propInfo.Band);
                                        var ulCenterFreq = CalculateULCenterFreq(dlCenterFreq, propInfo.Technology, propInfo.Band);

                                        // Get municipality for this cell's location
                                        var (municipalityId, municipalityName) = await GetMunicipalityByLocationAsync(location, token);
                                        
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
                                            linkCache,
                                            dlCenterFreq,
                                            ulCenterFreq,
                                            bandwidth,
                                            token);

                                        // Only create cellular if we have a valid network (links exist)
                                        if (networkId == Guid.Empty)
                                        {
                                            _logger.LogWarning(
                                                "Cannot create Cellular record for CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band} because required uplink/downlink not found for ServiceProvider: {ServiceProviderId}",
                                                sectorId, propInfo.Technology, propInfo.Band, serviceProviderId);
                                            continue; // Skip this cellular record
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
                    }
                }
            }
        }

        // --- Step 5.5: Validate All Cell Sectors Have Cellular Entities ---
        _logger.LogInformation("Validating cell sector to cellular entity completeness...");
        await ValidateAndCreateMissingCellularEntities(cellularToAdd, cellularLookup, cellSectorCache, cellSectorsCreatedForCellId, 
            serviceProviderCache, technologyCache, bandCache, linkCache, token);

        // --- Step 6: Batch Save All Accumulated Changes ---

        // Batch add new networks (before cellular records since they depend on networks)
        if (networksToAdd.Count != 0)
        {
            _networkRepository.AddRange(networksToAdd);
            await _networkRepository.SaveChangesAsync(token);
            _logger.LogInformation("{Count} new networks saved successfully (batched).", networksToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new networks to add (batched).");
        }

        // Batch add new cells
        if (cellsToAdd.Count != 0) // Corrected: Check if the list contains items
        {
            _cellRepository.AddRange(cellsToAdd); // Add all new cells to EF Core's change tracker
            await _cellRepository.SaveChangesAsync(token); // Save all new cells in one batch
            _logger.LogInformation("{Count} new cells saved successfully (batched).", cellsToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new cells to add (batched).");
        }

        // Batch add new cell sectors
        if (cellSectorList.Count != 0)
        {
            _cellSectorRepository.AddRange(cellSectorList);
            await _cellSectorRepository.SaveChangesAsync(token);
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
            _cellularRepository.AddRange(cellularToAdd);
            await _cellularRepository.SaveChangesAsync(token);
            _logger.LogInformation("{Count} cellular records saved successfully (batched).", cellularToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No cellular records to add.");
        }

        // Batch delete cellular entries
        if (cellularToDelete.Count != 0)
        {
            _cellularRepository.RemoveRange(cellularToDelete);
            await _cellularRepository.SaveChangesAsync(token);
            _logger.LogInformation("{Count} cellular records deleted successfully (batched).", cellularToDelete.Count);
        }

        // --- Step 7: Final File Status Update ---
        file.Status = FileStatusEnum.Success;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        _logger.LogInformation("Processing completed.");
    }

    /// <summary>
    /// Validates that all cell sectors have corresponding cellular entities for all active technology/band combinations
    /// and creates missing ones if needed.
    /// </summary>
    private async Task ValidateAndCreateMissingCellularEntities(
        List<Domain.Entities.Cellular> cellularToAdd,
        Dictionary<(Guid CellSectorId, Guid ServiceProviderId, Guid TechnologyId, Guid BandId), Domain.Entities.Cellular> cellularLookup,
        Dictionary<Guid, List<Guid>> cellSectorCache,
        Dictionary<Guid, List<Guid>> cellSectorsCreatedForCellId,
        Dictionary<string, Guid> serviceProviderCache,
        Dictionary<string, Guid> technologyCache,
        Dictionary<string, Guid> bandCache,
        Dictionary<string, Guid> linkCache,
        CancellationToken token)
    {
        var missingCellularCount = 0;

        // Get all cell sectors (existing + newly created)
        var allCellSectors = new Dictionary<Guid, List<Guid>>();
        
        // Add existing sectors
        foreach (var kvp in cellSectorCache)
        {
            allCellSectors[kvp.Key] = new List<Guid>(kvp.Value);
        }
        
        // Add newly created sectors
        foreach (var kvp in cellSectorsCreatedForCellId)
        {
            if (!allCellSectors.ContainsKey(kvp.Key))
                allCellSectors[kvp.Key] = new List<Guid>();
            allCellSectors[kvp.Key].AddRange(kvp.Value);
        }

        // Get all cells for wireless service providers
        var cellsWithProviders = await _cellRepository
            .Query(c => serviceProviderCache.Values.Contains(c.ServiceProviderId))
            .Select(c => new { c.Id, c.ServiceProviderId })
            .ToListAsync(token);

        foreach (var cell in cellsWithProviders)
        {
            if (!allCellSectors.TryGetValue(cell.Id, out var sectorIds) || sectorIds.Count == 0)
                continue;

            foreach (var sectorId in sectorIds)
            {
                // Check each technology/band combination
                foreach (var techId in technologyCache.Values)
                {
                    foreach (var bandId in bandCache.Values)
                    {
                        var key = (sectorId, cell.ServiceProviderId, techId, bandId);
                        
                        // If no cellular entity exists for this combination, create one with default values
                        if (!cellularLookup.ContainsKey(key))
                        {
                            var technology = technologyCache.FirstOrDefault(x => x.Value == techId).Key ?? "Unknown";
                            var band = bandCache.FirstOrDefault(x => x.Value == bandId).Key ?? "Unknown";
                            
                            // Only create cellular entities for valid technology/band combinations
                            if (technology != "Unknown" && band != "Unknown")
                            {
                                // For missing cellular entities, we'll use a default network or create one
                                // Since we don't have location context here, we'll use a default municipality
                                var defaultDLCenterFreq = GetDefaultDLCenterFreq(technology, band);
                                var defaultBandwidth = GetDefaultBandwidth(technology, band);
                                var defaultULCenterFreq = CalculateULCenterFreq(defaultDLCenterFreq, technology, band);

                                var defaultNetworkId = await GetOrCreateNetworkAsync(
                                    cell.ServiceProviderId,
                                    "Default", // Default municipality for system-created entities
                                    techId,
                                    bandId,
                                    new Dictionary<string, Guid>(), // Empty cache since this is for missing entities
                                    cellularToAdd.Count > 0 ? new List<Domain.Aggregates.Network>() : new List<Domain.Aggregates.Network>(), // Create new list to avoid modifying existing
                                    technologyCache,
                                    bandCache,
                                    linkCache,
                                    defaultDLCenterFreq,
                                    defaultULCenterFreq,
                                    defaultBandwidth,
                                    token);

                                // Only create cellular if we have a valid network (links exist)
                                if (defaultNetworkId == Guid.Empty)
                                {
                                    _logger.LogWarning(
                                        "Cannot create missing Cellular record for CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band} because required uplink/downlink not found for ServiceProvider: {ServiceProviderId}",
                                        sectorId, technology, band, cell.ServiceProviderId);
                                    continue; // Skip this cellular record
                                }

                                var newCellular = new Domain.Entities.Cellular
                                {
                                    CellSectorId = sectorId,
                                    ServiceProviderId = cell.ServiceProviderId,
                                    TechnologyId = techId,
                                    BandId = bandId,
                                    NetworkId = defaultNetworkId,
                                    Address = "", // Will be updated if needed
                                    IsFileUploaded = false, // Mark as system-created, not file-uploaded
                                    DLCenterFreq = defaultDLCenterFreq,
                                    Bandwidth = defaultBandwidth,
                                    ULCenterFreq = defaultULCenterFreq
                                };
                                
                                cellularToAdd.Add(newCellular);
                                cellularLookup[key] = newCellular;
                                missingCellularCount++;
                            }
                        }
                    }
                }
            }
        }

        if (missingCellularCount > 0)
        {
            _logger.LogInformation("Created {Count} missing cellular entities to ensure completeness.", missingCellularCount);
        }
    }

    /// <summary>
    /// Gets default DL center frequency based on technology and band when not specified in Excel
    /// </summary>
    private static decimal GetDefaultDLCenterFreq(string technology, string band)
    {
        // Return technology and band specific default frequencies or fallback to 0
        return (technology.ToUpperInvariant(), band) switch
        {
            ("GSM", "Band 8") => 947.5m,    // 900 MHz GSM center
            ("GSM", "Band 3") => 1842.5m,   // 1800 MHz GSM center
            ("WCDMA", "Band 5") => 869m,    // 850 MHz WCDMA center
            ("WCDMA", "Band 8") => 950m,    // 900 MHz WCDMA center
            ("WCDMA", "Band 1") => 2140m,   // 2100 MHz WCDMA center
            ("LTE-FDD", "Band 28") => 758m, // 700 MHz LTE center
            ("LTE-FDD", "Band 5") => 869m,  // 850 MHz LTE center
            ("LTE-FDD", "Band 8") => 950m,  // 900 MHz LTE center
            ("LTE-FDD", "Band 3") => 1842.5m, // 1800 MHz LTE center
            ("LTE-FDD", "Band 1") => 2140m, // 2100 MHz LTE center
            ("LTE-TDD", "Band 40") => 2300m, // 2300 MHz TDD center
            ("LTE-TDD", "Band 41") => 2500m, // 2500 MHz TDD center
            ("LTE-TDD", "Band 38") => 2600m, // 2600 MHz TDD center
            ("NR", "Band 78") => 3500m,     // 3500 MHz NR center
            ("NR", "Band 38") => 2600m,     // 2600 MHz NR center
            _ => DEFAULT_DL_CENTER_FREQ      // Fallback to 0
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
            "GSM" => 0.2m,      // 200 kHz
            "WCDMA" => 5m,      // 5 MHz
            "LTE-FDD" => 20m,   // 20 MHz
            "LTE-TDD" => 20m,   // 20 MHz
            "NR" => 100m,       // 100 MHz for 5G
            _ => DEFAULT_BANDWIDTH // 20 MHz default
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
        _logger.LogInformation("Process terminated due to error in runtime");
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
            "WCDMA" => CalculateWcdmaULFreq(dlCenterFreq, band),
            "LTE-FDD" => CalculateLteFddULFreq(dlCenterFreq, band),
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

    private static string GetBandFromPropertyName(string propertyName)
    {
        if (propertyName.Contains("700")) return "Band 28";
        if (propertyName.Contains("850")) return "Band 5";
        if (propertyName.Contains("900")) return "Band 8";
        if (propertyName.Contains("1800")) return "Band 3";
        if (propertyName.Contains("2100")) return "Band 1";
        if (propertyName.Contains("2300")) return "Band 40";
        if (propertyName.Contains("2500")) return "Band 41";
        if (propertyName.Contains("2600")) return "Band 38";
        if (propertyName.Contains("3400")) return "Band 42";
        if (propertyName.Contains("3500")) return "Band 78";
        return string.Empty;
    }

    private static string GetGenerationFromPropertyName(string propertyName)
    {
        if (propertyName.StartsWith("Gsm")) return "2G";
        if (propertyName.StartsWith("Wcdma")) return "3G";
        if (propertyName.StartsWith("FddLte") || propertyName.StartsWith("TddLte")) return "4G";
        if (propertyName.StartsWith("Nr")) return "5G";
        return string.Empty;
    }

    private static string GetTechnologyFromPropertyName(string propertyName)
    {
        if (propertyName.StartsWith("Gsm")) return "GSM";
        if (propertyName.StartsWith("Wcdma")) return "WCDMA";
        if (propertyName.StartsWith("FddLte")) return "LTE-FDD";
        if (propertyName.StartsWith("TddLte")) return "LTE-TDD";
        if (propertyName.StartsWith("Nr")) return "NR";
        return string.Empty;
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
    /// Gets municipality by location using spatial intersection
    /// </summary>
    private async Task<(Guid? Id, string Name)> GetMunicipalityByLocationAsync(Point location, CancellationToken token)
    {
        try
        {
            var municipality = await _municipalityRepository
                .Query(m => m.Boundaries != null && m.Boundaries.Intersects(location))
                .Select(m => new { m.Id, m.Name })
                .FirstOrDefaultAsync(token);

            if (municipality != null)
            {
                _logger.LogDebug("Found municipality: {MunicipalityName} for location: {Location}", 
                    municipality.Name, location);
                return (municipality.Id, municipality.Name);
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
        Dictionary<string, Guid> linkCache,
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
                "SystemType not found for Technology: {TechnologyInfo}. " +
                "Cannot create network without existing SystemType. " +
                "Please ensure SystemType exists in database for ServiceProvider: {ServiceProviderId}, " +
                "Municipality: {MunicipalityName}, Technology: {Technology}, Band: {Band}",
                technologyInfo, serviceProviderId, municipalityName, technologyName, bandName);

            return Guid.Empty;
        }

        // Look up existing uplink and downlink using pre-loaded cache
        var uplinkKey = $"{serviceProviderId}_{bandId}_{LinkTypeEnum.Uplink}";
        var downlinkKey = $"{serviceProviderId}_{bandId}_{LinkTypeEnum.Downlink}";

        if (!linkCache.TryGetValue(uplinkKey, out var uplinkId) || 
            !linkCache.TryGetValue(downlinkKey, out var downlinkId))
        {
            _logger.LogWarning(
                "Required links not found for ServiceProvider: {ServiceProviderId}, Band: {BandId}. " +
                "Uplink exists: {UplinkExists}, Downlink exists: {DownlinkExists}. " +
                "Cannot create network without existing links.",
                serviceProviderId, bandId, linkCache.ContainsKey(uplinkKey), linkCache.ContainsKey(downlinkKey));

            return Guid.Empty;
        }

        var network = new Domain.Aggregates.Network
        {
            Name = $"{technologyName}-{bandName}-{municipalityName}",
            ServiceProviderId = serviceProviderId,
            SystemTypeId = systemType.Id,
            UpLinkId = uplinkId,
            DownLinkId = downlinkId,
            ULCenterFreq = ulCenterFreq,
            DLCenterFreq = dlCenterFreq,
            Bandwidth = bandwidth
        };

        networksToAdd.Add(network);
        networkCache[networkCacheKey] = network.Id;

        _logger.LogInformation(
            "Created new Network: {NetworkName} for ServiceProvider: {ServiceProviderId}, Municipality: {MunicipalityName}, Technology: {Technology}, Band: {Band}, UplinkId: {UplinkId}, DownlinkId: {DownlinkId}",
            network.Name, serviceProviderId, municipalityName, technologyName, bandName, uplinkId, downlinkId);

        return network.Id;
    }
}