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
    private readonly IGenerationBandRepository _generationBandRepository;
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
        _generationBandRepository = generationBandRepository;
        _cellSectorRepository = cellSectorRepository;
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file == null)
        {
            const string message = "File not found.";
            _logger.LogError(message);
            throw new Exception(message);
        };

        file.Status = FileStatusEnum.Processing;

        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        var serviceProviderCache = new Dictionary<string, Guid>();
        var generationBandCache = new Dictionary<(string, string), Guid>();

        var cellSectorList = new List<Domain.Entities.CellSector>();
        var cellularToAdd = new List<Domain.Entities.Cellular>();
        var cellularToDelete = new List<Domain.Entities.Cellular>();

        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        _logger.LogInformation("Processing file: {FilePath}", parameters.Path);

        fileStream.Position = 0;
        _logger.LogInformation("Processing started");

        var cellRecords = CellExcelHelper.ParseExcelToCellRecords(fileStream);

        if (cellRecords.Count is 0)
        {
            _logger.LogInformation("The Excel is empty.");
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

        for (int recordIndex = 0; recordIndex < cellRecords.Count; recordIndex++)
        {
            var record = cellRecords[recordIndex];
            var actualRowNumber = recordIndex + 4; // Adjust for the starting row

            _logger.LogInformation("Processing record from Excel row {RowNumber}: {Provider}, {Zone} {Barangay} {Municipality} {Province} {Region}, Coordinates: ({Latitude}, {Longitude})",
                actualRowNumber, record.Provider, record.Zone, record.Barangay, record.Municipality, record.Province, record.Region, record.Latitude, record.Longitude);

            ReportProgress($"Processing record from Excel row {actualRowNumber}: {record.Provider}, {record.Zone} {record.Barangay} {record.Municipality} {record.Province} {record.Region}, Coordinates: ({record.Latitude}, {record.Longitude})");
            var spCacheKey = (record.Provider + ServiceTypeEnum.Wireless.ToString());

            if (!serviceProviderCache.TryGetValue(spCacheKey, out var serviceProviderId))
            {
                var serviceProviderQuery = _serviceProviderRepository
                    .Query(sp => !sp.IsDeleted)
                    .Where(sp => sp.Provider.Name.ToLower() == record.Provider.ToLower() && sp.Service.Type == ServiceTypeEnum.Wireless);

                var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken: token);

                if (serviceProvider is not null)
                {
                    serviceProviderId = serviceProvider.Id;
                    serviceProviderCache[spCacheKey] = serviceProviderId;
                }
                else
                {
                    _logger.LogWarning("Wireless Service Provider not found: {Provider}", record.Provider);
                    await TerminateProcess(file, token);
                    return;
                }
            }

            Point location = GeometryHelper.CreatePoint(y: record.Latitude, x: record.Longitude);
            string address = $"{record.Zone}, {record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";

            Guid cellId = Guid.Empty;
            if (serviceProviderId != Guid.Empty)
            {
                var cellQuery = _cellRepository
                    .Query(c => !c.IsDeleted && c.ServiceProviderId.Equals(serviceProviderId))
                    .Where(c => c.TowerType.Equals(record.TowerType))
                    .Where(c => c.Location.Equals(location));

                var existingCell = await _cellRepository.FirstOrDefaultAsync(cellQuery, token);
                if (existingCell != null)
                {
                    _logger.LogWarning("Cell already exists for Service Provider: {Provider}, Location: {location}", record.Provider, location);
                    cellId = existingCell.Id;
                }
                else
                {
                    var cell = new Domain.Aggregates.Cell
                    {
                        Name = $"Cell-{record.Provider}-{record.Zone}-{record.Municipality}-{record.Region}",
                        Group = record.Provider,
                        Address = address,
                        ServiceProviderId = serviceProviderId,
                        TowerType = record.TowerType,
                        Location = location,
                        IsFileUploaded = true,
                    };
                    _cellRepository.Add(cell);

                    cellId = cell.Id;
                    _logger.LogInformation("Cell {CellId} saved successfully.", cellId);

                    if (!String.IsNullOrEmpty(record.Azimuth))
                    {
                        var azimuths = record.Azimuth.Split(",");
                        var beamTilts = record.BeamTilt?.Split(',');

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
            }
            else
            {
                _logger.LogWarning("Address not found: {Zone} in Barangay: {Barangay}, Municipality: {Municipality}, Province: {Province}, Region: {Region}",
                                   record.Zone, record.Barangay, record.Municipality, record.Province, record.Region);
                await TerminateProcess(file, token);
                return;
            }

            if (cellId != Guid.Empty)
            {
                foreach (var propInfo in bandGenerationProperties)
                {
                    var generationBandCacheKey = (propInfo.Generation, propInfo.Band);

                    if (!generationBandCache.TryGetValue(generationBandCacheKey, out var generationBandId))
                    {
                        var generationBand = await _generationBandRepository.GetGenerationBandIdAsync(propInfo.Generation, propInfo.Band, token);

                        if (generationBand != null)
                        {
                            generationBandId = generationBand.Id;
                            generationBandCache[generationBandCacheKey] = generationBandId;
                        }
                        else
                        {
                            _logger.LogWarning("Generation Band not found: Generation: {Generation}, Band: {Band}", propInfo.Generation, propInfo.Band);
                            await TerminateProcess(file, token);
                            return;
                        }
                    }

                    if (generationBandId != Guid.Empty)
                    {
                        var existingCellular = await _cellularRepository.Query(c =>
                                !c.IsDeleted &&
                                c.CellId.Equals(cellId) &&
                                c.ServiceProviderId.Equals(serviceProviderId) &&
                                c.GenerationBandId.Equals(generationBandId))
                            .FirstOrDefaultAsync(token);

                        var value = propInfo.Property.GetValue(record);
                        bool isBandActiveInExcel = value is not null && (bool)value;

                        if (existingCellular != null)
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
                                cellularToAdd.Add(new Domain.Entities.Cellular
                                {
                                    CellId = cellId,
                                    ServiceProviderId = serviceProviderId,
                                    GenerationBandId = generationBandId,
                                    Address = address,
                                    IsFileUploaded = true
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Cell not found nor created.");
                await TerminateProcess(file, token);
                return;
            }
        }

        file.Status = FileStatusEnum.Success;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        await _cellRepository.SaveChangesAsync(token);

        if (cellSectorList.Count != 0)
        {
            _cellSectorRepository.AddRange(cellSectorList);
            await _cellSectorRepository.SaveChangesAsync(token);
            _logger.LogInformation("{Count} sector records saved successfully.", cellSectorList.Count);
        }
        else
        {
            _logger.LogInformation("No sector records to add.");
        }

        if (cellularToAdd.Count != 0)
        {
            _cellularRepository.AddRange(cellularToAdd);
            await _cellularRepository.SaveChangesAsync(token);
            _logger.LogInformation("{Count} cellular records saved successfully.", cellularToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No cellular records to add.");
        }

        if (cellularToDelete.Count != 0)
        {
            _cellularRepository.RemoveRange(cellularToDelete);
            await _cellularRepository.SaveChangesAsync(token);
            _logger.LogInformation("{Count} cellular records deleted successfully.", cellularToDelete.Count);
        }

        _logger.LogInformation("Processing completed.");
    }
    
    void ReportProgress(string message)
    {
        // Implement the logic to report progress here
        // For example, you might want to log the progress or update a progress bar in the UI
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
