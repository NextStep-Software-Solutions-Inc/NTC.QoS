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

    public ProcessCellJob(
        ILogger<ProcessCellJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Cell> cellRepository,
        IRepository<Domain.Entities.Cellular> cellularRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IGenerationBandRepository generationBandRepository,
        IRepository<Domain.Entities.CellSector> cellSectorRepository,
        ITechnologyRepository technologyRepository,
        IBandRepository bandRepository
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

        // Pre-load 
        var technologyCache = await _technologyRepository
            .Query()
            .Select(t => new
            {
                t.Id,
                Technology = t.Standard
            })
            .ToDictionaryAsync(
                t => CacheKeyGenerator.GenerateTechnologyCacheKey(t.Technology),
                t => t.Id,
                token
            );
        _logger.LogInformation("Pre-loaded {Count} technologies.", technologyCache.Count);

        var bandCache = await _bandRepository
            .Query()
            .Select(b => new
            {
                b.Id,
                Band = b.Name
            })
            .ToDictionaryAsync(
                b => CacheKeyGenerator.GenerateBandCacheKey(b.Band), // Fixed: use GenerateBandCacheKey instead of GenerateTechnologyCacheKey
                b => b.Id,
                token
            );
        _logger.LogInformation("Pre-loaded {Count} bands.", bandCache.Count); // Fixed: log message for bands

        var providerIds = serviceProviderCache.Values.ToList();

        var cellCache = await _cellRepository
            .Query(c => providerIds.Contains(c.ServiceProviderId))
            .ToDictionaryAsync(
                c => CacheKeyGenerator.GenerateCellCacheKey(c.ServiceProviderId, c.TowerType, c.Location),
                c => c.Id,
                token
            );
        _logger.LogInformation("Pre-loaded {Count} existing cells.", cellCache.Count);

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

            var cellCacheKey = CacheKeyGenerator.GenerateCellCacheKey(serviceProviderId, record.TowerType, location);

            if (!cellCache.TryGetValue(cellCacheKey, out Guid cachedCellId))
            {
                // Cell not in cache, meaning it's a new cell.
                var cell = new Domain.Aggregates.Cell
                {
                    Name = $"Cell-{record.Provider}-{address}-{record.TowerType}-{record.Longitude}-{record.Latitude}",
                    Group = record.Provider,
                    Address = address,
                    ServiceProviderId = serviceProviderId,
                    TowerType = record.TowerType,
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
                        _logger.LogWarning("Technology not found in pre-loaded cache: {Technology}. Terminating process.", propInfo.Technology);
                        await TerminateProcess(file, token);
                        return;
                    }

                    var bandCacheKey = CacheKeyGenerator.GenerateBandCacheKey(propInfo.Band);
                    if (!bandCache.TryGetValue(bandCacheKey, out var bandId))
                    {
                        _logger.LogWarning("Band not found in pre-loaded cache: {Band}. Terminating process.", propInfo.Band);
                        await TerminateProcess(file, token);
                        return;
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
                            if (cellSectorCache.TryGetValue(cellId, out var allSectorIds))
                            {
                                _logger.LogInformation("Found {SectorCount} sector(s) for CellId: {CellId}", allSectorIds.Count, cellId);

                                foreach (var sectorId in allSectorIds)
                                {
                                    var key = (sectorId, serviceProviderId, technologyId, bandId);
                                    if (!cellularLookup.ContainsKey(key))
                                    {
                                        var newCellular = new Domain.Entities.Cellular
                                        {
                                            CellSectorId = sectorId,
                                            ServiceProviderId = serviceProviderId,
                                            TechnologyId = technologyId,
                                            BandId = bandId,
                                            Address = address,
                                            IsFileUploaded = true,
                                            DLCenterFreq = parseResult.DLCenterFreq ?? 0,
                                            Bandwidth = parseResult.Bandwidth ?? 0
                                        };

                                        // Calculate UL center frequency if DL is provided
                                        if (parseResult.DLCenterFreq.HasValue)
                                        {
                                            newCellular.ULCenterFreq = CalculateULCenterFreq(parseResult.DLCenterFreq.Value, propInfo.Technology, propInfo.Band);
                                        }

                                        cellularToAdd.Add(newCellular);
                                        cellularLookup[key] = newCellular;

                                        _logger.LogInformation(
                                            "Marked Cellular record for addition: CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band}, DLCenterFreq: {DLCenterFreq}, ULCenterFreq: {ULCenterFreq}, Bandwidth: {Bandwidth}",
                                            sectorId, propInfo.Technology, propInfo.Band, newCellular.DLCenterFreq, newCellular.ULCenterFreq, newCellular.Bandwidth);
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
                                // Check if we have sectors from new cells created in this batch
                                if (cellSectorsCreatedForCellId.TryGetValue(cellId, out var newSectorIds))
                                {
                                    _logger.LogInformation("Found {SectorCount} newly created sector(s) for CellId: {CellId}", newSectorIds.Count, cellId);

                                    foreach (var sectorId in newSectorIds)
                                    {
                                        var key = (sectorId, serviceProviderId, technologyId, bandId);
                                        if (!cellularLookup.ContainsKey(key))
                                        {
                                            var newCellular = new Domain.Entities.Cellular
                                            {
                                                CellSectorId = sectorId,
                                                ServiceProviderId = serviceProviderId,
                                                TechnologyId = technologyId,
                                                BandId = bandId,
                                                Address = address,
                                                IsFileUploaded = true,
                                                DLCenterFreq = parseResult.DLCenterFreq ?? 0,
                                                Bandwidth = parseResult.Bandwidth ?? 0
                                            };

                                            // Calculate UL center frequency if DL is provided
                                            if (parseResult.DLCenterFreq.HasValue)
                                            {
                                                newCellular.ULCenterFreq = CalculateULCenterFreq(parseResult.DLCenterFreq.Value, propInfo.Technology, propInfo.Band);
                                            }

                                            cellularToAdd.Add(newCellular);
                                            cellularLookup[key] = newCellular;

                                            _logger.LogInformation(
                                                "Marked Cellular record for addition (new cell): CellSectorId: {CellSectorId}, Technology: {Technology}, Band: {Band}, DLCenterFreq: {DLCenterFreq}, ULCenterFreq: {ULCenterFreq}, Bandwidth: {Bandwidth}",
                                                sectorId, propInfo.Technology, propInfo.Band, newCellular.DLCenterFreq, newCellular.ULCenterFreq, newCellular.Bandwidth);
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
        }

        // --- Step 6: Batch Save All Accumulated Changes ---

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
}