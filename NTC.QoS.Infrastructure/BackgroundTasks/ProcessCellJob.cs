using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Attributes;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;
using System.Reflection;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessCellJob : IProcessCellJob
{
    private readonly ILogger<ProcessCellJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.Cell> _cellRepository;
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Domain.Entities.Zone> _zoneRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;
    private readonly IGenerationBandRepository _generationBandRepository;
    private readonly IRepository<Domain.Entities.Sector> _sectorRepository;

    public ProcessCellJob(
        ILogger<ProcessCellJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Cell> cellRepository,
        IRepository<Domain.Entities.Cellular> cellularRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.Zone> zoneRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository,
        IGenerationBandRepository generationBandRepository,
        IRepository<Domain.Entities.Sector> sectorRepository
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _cellRepository = cellRepository;
        _cellularRepository = cellularRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _zoneRepository = zoneRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _generationBandRepository = generationBandRepository;
        _sectorRepository = sectorRepository;
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file == null)
        {
            const string message = "File not found";
            _logger.LogError(message);
            throw new Exception(message);
        }
        ;

        file.Status = FileStatusEnum.Processing;

        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        var serviceProviderCache = new Dictionary<string, Guid>();
        var zoneCache = new Dictionary<string, Guid>();
        var barangayCache = new Dictionary<string, Guid>();
        var municipalityCache = new Dictionary<string, Guid>();
        var provinceCache = new Dictionary<string, Guid>();
        var regionCache = new Dictionary<string, Guid>();
        var generationBandCache = new Dictionary<(string, string), Guid>();

        var sectorList = new List<Domain.Entities.Sector>();
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

        foreach (var record in cellRecords)
        {
            _logger.LogInformation("Processing record: {Provider}, {Zone} {Barangay} {Municipality} {Province} {Region}, Coordinates: ({Latitude}, {Longitude})",
                record.Provider, record.Zone, record.Barangay, record.Municipality, record.Province, record.Region, record.Latitude, record.Longitude);

            ReportProgress($"Processing record: {record.Provider}, {record.Zone} {record.Barangay} {record.Municipality} {record.Province} {record.Region}, Coordinates: ({record.Latitude}, {record.Longitude})");

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

            if (!regionCache.TryGetValue(record.Region, out var regionId))
            {
                var region = await _regionRepository.Query(r => !r.IsDeleted && r.Name.Equals(record.Region)).FirstOrDefaultAsync(token);
                if (region != null)
                {
                    regionId = region.Id;
                    regionCache[record.Region] = regionId;
                }
                else
                {
                    _logger.LogWarning("Region not found: {Region}", record.Region);
                    await TerminateProcess(file, token);
                    return;
                }
            }

            var provinceCacheKey = $"{record.Province}-{regionId}";
            if (!provinceCache.TryGetValue(provinceCacheKey, out var provinceId))
            {
                var province = await _provinceRepository.Query(p => !p.IsDeleted && p.Name.Equals(record.Province) && p.RegionId.Equals(regionId)).FirstOrDefaultAsync(token);
                if (province != null)
                {
                    provinceId = province.Id;
                    provinceCache[provinceCacheKey] = provinceId;
                }
                else
                {
                    _logger.LogWarning("Province not found: {Province} in Region: {Region}", record.Province, record.Region);
                    await TerminateProcess(file, token);
                    return;
                }
            }

            var municipalityCacheKey = $"{record.Municipality}-{provinceId}";
            if (!municipalityCache.TryGetValue(municipalityCacheKey, out var municipalityId))
            {
                var municipality = await _municipalityRepository.Query(m => !m.IsDeleted && m.Name.Equals(record.Municipality) && m.ProvinceId.Equals(provinceId)).FirstOrDefaultAsync(token);
                if (municipality != null)
                {
                    municipalityId = municipality.Id;
                    municipalityCache[municipalityCacheKey] = municipalityId;
                }
                else
                {
                    _logger.LogWarning("Municipality not found: {Municipality} in Province: {Province}, Region: {Region}", record.Municipality, record.Province, record.Region);
                    await TerminateProcess(file, token);
                    return;
                }
            }

            var barangayCacheKey = $"{record.Barangay}-{municipalityId}";
            if (!barangayCache.TryGetValue(barangayCacheKey, out var barangayId))
            {
                var barangay = await _barangayRepository.Query(b => !b.IsDeleted && b.Name.Equals(record.Barangay) && b.MunicipalityId.Equals(municipalityId)).FirstOrDefaultAsync(token);
                if (barangay != null)
                {
                    barangayId = barangay.Id;
                    barangayCache[barangayCacheKey] = barangayId;
                }
                else
                {
                    _logger.LogWarning("Barangay not found: {Barangay} in Municipality: {Municipality}, Province: {Province}, Region: {Region}", record.Barangay, record.Municipality, record.Province, record.Region);
                    await TerminateProcess(file, token);
                    return;
                }
            }

            var zoneCacheKey = $"{record.Zone}-{barangayId}";
            if (!zoneCache.TryGetValue(zoneCacheKey, out var zoneId))
            {
                var zone = await _zoneRepository.Query(z => !z.IsDeleted && z.Name.Equals(record.Zone) && z.BarangayId.Equals(barangayId)).FirstOrDefaultAsync(token);
                if (zone != null)
                {
                    zoneId = zone.Id;
                    zoneCache[zoneCacheKey] = zoneId;
                }
                else
                {
                    _logger.LogWarning("Zone not found: {Zone} in Barangay: {Barangay}, Municipality: {Municipality}, Province: {Province}, Region: {Region}",
                                       record.Zone, record.Barangay, record.Municipality, record.Province, record.Region);
                    await TerminateProcess(file, token);
                    return;
                }
            }

            Guid cellId = Guid.Empty;
            if (serviceProviderId != Guid.Empty && municipalityId != Guid.Empty && zoneId != Guid.Empty)
            {
                var cellQuery = _cellRepository
                    .Query(c => !c.IsDeleted && c.ServiceProviderId.Equals(serviceProviderId) && c.ZoneId.Equals(zoneId))
                    .Where(c => c.TowerType.Equals(record.TowerType));

                var existingCell = await _cellRepository.FirstOrDefaultAsync(cellQuery, token);
                if (existingCell != null)
                {
                    _logger.LogWarning("Cell already exists for Service Provider: {Provider}, Zone: {Zone}", record.Provider, record.Zone);
                    cellId = existingCell.Id;
                }
                else
                {
                    var cell = new Domain.Aggregates.Cell
                    {
                        Name = $"Cell-{record.Provider.Trim()}-{record.Zone.Trim()}-{record.Municipality.Trim()}-{record.Region.Trim()}",
                        ServiceProviderId = serviceProviderId,
                        ZoneId = zoneId,
                        FileId = parameters.Id,
                        CoorLatitude = record.Latitude.ToString(),
                        CoorLongitude = record.Longitude.ToString(),
                        TowerType = record.TowerType,
                        IsFileUploaded = true,
                    };
                    _cellRepository.Add(cell);
                    await _cellRepository.SaveChangesAsync(token);

                    cellId = cell.Id;
                    _logger.LogInformation("Cell {CellId} saved successfully.", cellId);
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
                                c.GenerationBandId.Equals(generationBandId) &&
                                c.BarangayId.Equals(barangayId))
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
                                    FileId = parameters.Id,
                                    ServiceProviderId = serviceProviderId,
                                    BarangayId = barangayId,
                                    GenerationBandId = generationBandId,
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

        //if (sectorList.Count != 0)
        //{
        //    _sectorRepository.AddRange(sectorList);
        //    await _sectorRepository.SaveChangesAsync(token);
        //    _logger.LogInformation("Sector records saved successfully.");
        //}
        //else
        //{
        //    _logger.LogInformation("No sector records to add OR sector under a cell record already exists.");
        //}

        if (cellularToAdd.Count != 0)
        {
            _cellularRepository.AddRange(cellularToAdd);
            await _cellularRepository.SaveChangesAsync(token);
            _logger.LogInformation("{cellularToAdd.Count} cellular records saved successfully.", cellularToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No cellular records to add.");
        }

        if (cellularToDelete.Count != 0)
        {
            _cellularRepository.RemoveRange(cellularToDelete);
            await _cellularRepository.SaveChangesAsync(token);
            _logger.LogInformation("{cellularToDelete.Count} cellular records deleted successfully.", cellularToDelete.Count);
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
