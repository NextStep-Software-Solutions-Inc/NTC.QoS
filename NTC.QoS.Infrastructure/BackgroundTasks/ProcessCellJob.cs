using System.Reflection;
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

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessCellJob : IProcessCellJob
{
    private readonly ILogger<ProcessCellJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.Cell> _cellRepository;
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IGenerationBandRepository _generationBandRepository; // Keep this as you have it
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository;

    public ProcessCellJob(
        ILogger<ProcessCellJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Cell> cellRepository,
        IRepository<Domain.Entities.Cellular> cellularRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IGenerationBandRepository generationBandRepository,
        IRepository<Domain.Entities.CellSector> cellSectorRepository
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _cellRepository = cellRepository;
        _cellularRepository = cellularRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _generationBandRepository = generationBandRepository; // Keep this as you have it
        _cellSectorRepository = cellSectorRepository;
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        // --- Step 1: Initial File Status Update ---
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file == null)
        {
            const string message = "File not found.";
            _logger.LogError(message);
            throw new Exception(message);
        }
        ;

        file.Status = FileStatusEnum.Processing;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        //Step 2: Initialize Caches and Lists for Batch Operations ---
        var serviceProviderCache = new Dictionary<string, Guid>();
        var generationBandCache = new Dictionary<(string, string), Guid>();
        var cellCache = new Dictionary<string, Guid>();

        var cellSectorList = new List<Domain.Entities.CellSector>();
        var cellularToAdd = new List<Domain.Entities.Cellular>();
        var cellularToDelete = new List<Domain.Entities.Cellular>();

        // IMPORTANT: Declare cellsToAdd list here, BEFORE the main loop,
        // so it can accumulate new cells across all records.
        var cellsToAdd = new List<Domain.Aggregates.Cell>();

        //Step 3: Pre-load Static/Reference Data for Performance
        _logger.LogInformation("Pre-loading reference data.");

        // Pre-load all wireless service providers
        serviceProviderCache = await _serviceProviderRepository
            .Query(sp => !sp.IsDeleted && sp.Service.Type == ServiceTypeEnum.Wireless)
            .Select(sp => new { sp.Id, sp.Provider.Name })
            .ToDictionaryAsync( // 1. Fetches data AND populates the dictionary in one go
                 sp => $"{sp.Name}-{ServiceTypeEnum.Wireless}".ToLowerInvariant(), 
                 sp => sp.Id,                                          
                 token
            );

        _logger.LogInformation("Pre-loaded {Count} wireless service providers.", serviceProviderCache.Count);

        // Pre-load all generation bands
        // Corrected pre-loading of generation bands
        generationBandCache = await _generationBandRepository
            .Query(gb => !gb.IsDeleted)
            .Select(gb => new
            {
                gb.Id,
                Generation = gb.Generation.Name, 
                Band = gb.Band.Name            
            })
            .ToDictionaryAsync(
                gb => (gb.Generation, gb.Band),
                gb => gb.Id,                    
                token
            );

        _logger.LogInformation("Pre-loaded {Count} generation bands.", generationBandCache.Count);

        var providerIds = serviceProviderCache.Values.ToList();

        cellCache = await _cellRepository
            .Query(c => !c.IsDeleted && providerIds.Contains(c.ServiceProviderId))
            .ToDictionaryAsync(
                c => ($"Cell-{c.ServiceProviderId}_{c.TowerType}_{c.Location.Coordinate.X}_{c.Location.Coordinate.Y}"),
                c => c.Id,
                token
            );

        _logger.LogInformation("Pre-loaded {Count} existing cells.", cellCache.Count);

        // Pre-load existing Cellular records for efficient in-memory checks.
        var cellularLookup = await _cellularRepository
            .Query(c => !c.IsDeleted && serviceProviderCache.Values.Contains(c.ServiceProviderId))
            .ToDictionaryAsync(
                c => (c.CellId, c.ServiceProviderId, c.GenerationBandId),
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

        var bandGenerationProperties = typeof(CellRecord).GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .Select(property => new
            {
                Property = property,
                HeaderAttribute = property.GetCustomAttribute<ExcelHeaderAttribute>(),
                Band = GetBandFromPropertyName(property.Name),
                Generation = GetGenerationFromPropertyName(property.Name)
            })
            .Where(item => item.HeaderAttribute != null && !string.IsNullOrEmpty(item.Band) && !string.IsNullOrEmpty(item.Generation))
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
            var spCacheKey = ($"{record.Provider}-{ServiceTypeEnum.Wireless}").ToLower();

            if (!serviceProviderCache.TryGetValue(spCacheKey, out var serviceProviderId))
            {
                // This log now genuinely means it's not found in the DB (or is not wireless)
                _logger.LogWarning("Wireless Service Provider not found in pre-loaded cache: {Provider}. Terminating process.", record.Provider);
                await TerminateProcess(file, token);
                return;
            }

            Point location = GeometryHelper.CreatePoint(new Coordinate(y: record.Latitude, x: record.Longitude));
            string address = $"{record.Zone}, {record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";

            Guid cellId = Guid.Empty;
            // The check for serviceProviderId != Guid.Empty is already handled by the `TryGetValue` above.
            // If we reach here, serviceProviderId is valid.

            var cellCacheKey = $"Cell-{serviceProviderId}_{record.TowerType}_{location.Coordinate.X}_{location.Coordinate.Y}";

            if (!cellCache.TryGetValue(cellCacheKey, out Guid cachedCellId))
            {
                // Cell not in cache, meaning it's a new cell.
                var cell = new Domain.Aggregates.Cell
                {
                    Name = $"Cell-{record.Provider}-{record.Zone}-{record.Municipality}-{record.Region}-{record.TowerType}-{record.Longitude}-{record.Latitude}",
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
            if (cellId != Guid.Empty) // Should always be true if we got cellId
            {
                foreach (var propInfo in bandGenerationProperties)
                {
                    var generationBandCacheKey = (propInfo.Generation, propInfo.Band);

                    if (!generationBandCache.TryGetValue(generationBandCacheKey, out var generationBandId))
                    {
                        _logger.LogWarning("Generation Band not found in pre-loaded cache: Generation: {Generation}, Band: {Band}. Terminating process.", propInfo.Generation, propInfo.Band);
                        await TerminateProcess(file, token);
                        return;
                    }

                    // Check for existing cellular record in-memory (using the pre-loaded lookup)
                    var value = propInfo.Property.GetValue(record);
                    bool isBandActiveInExcel = value is not null && (bool)value;

                    if (cellularLookup.TryGetValue((cellId, serviceProviderId, generationBandId), out var existingCellular))
                    {
                        if (!isBandActiveInExcel)
                        {
                            cellularToDelete.Add(existingCellular);
                            _logger.LogInformation("Marked Cellular record for deletion: CellularId: {CellularId}, CellId: {CellId}, Generation: {Generation}, Band: {Band}", existingCellular.Id, cellId, propInfo.Generation, propInfo.Band);
                        }
                    }
                    else
                    {
                        if (isBandActiveInExcel)
                        {
                            var newCellular = new Domain.Entities.Cellular
                            {
                                CellId = cellId,
                                ServiceProviderId = serviceProviderId,
                                GenerationBandId = generationBandId,
                                Address = address,
                                IsFileUploaded = true
                            };
                            cellularToAdd.Add(newCellular);
                            // Add to in-memory lookup immediately to prevent duplicate additions
                            cellularLookup[(newCellular.CellId, newCellular.ServiceProviderId, newCellular.GenerationBandId)] = newCellular;
                            _logger.LogInformation("Marked Cellular record for addition: CellId: {CellId}, Generation: {Generation}, Band: {Band}", cellId, propInfo.Generation, propInfo.Band);
                        }
                    }
                }
            }
        }

        // --- Step 6: Batch Save All Accumulated Changes ---

        // Batch add new cells
        if (cellsToAdd.Any()) // Corrected: Check if the list contains items
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
}