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
        IGenerationBandRepository generationBandRepository
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
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file == null)
        {
            var message = "File not found";
            _logger.LogError(message);
            throw new Exception(message);
        };

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

        var cellList = new List<Domain.Aggregates.Cell>();
        var cellularList = new List<Domain.Entities.Cellular>();

        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        _logger.LogInformation($"Processing file: {parameters.Path}");

        fileStream.Position = 0;
        _logger.LogInformation("Processing started");

        var cellRecords = CellExcelHelper.ParseExcelToCellRecords(fileStream);

        if (cellRecords.Count is 0)
        {
            _logger.LogInformation("The Excel is empty.");
        }

        foreach (var record in cellRecords)
        {
            _logger.LogInformation($"Processing record: {record}");

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
                    _logger.LogWarning($"Wireless Service Provider not found: {record.Provider}");
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
                    _logger.LogWarning($"Region not found: {record.Region}");
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
                    _logger.LogWarning($"Province not found: {record.Province} in Region: {record.Region}");
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
                    _logger.LogWarning($"Municipality not found: {record.Municipality} in Province: {record.Province}, Region: {record.Region}");
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
                    _logger.LogWarning($"Barangay not found: {record.Barangay} in Municipality: {record.Municipality}, Province: {record.Province}, Region: {record.Region}");
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
                    _logger.LogWarning($"Zone not found: {record.Zone} in Barangay: {record.Barangay}, Municipality: {record.Municipality}, Province: {record.Province}, Region: {record.Region}");
                    await TerminateProcess(file, token);
                    return;
                }
            }

            foreach (var property in typeof(CellRecord).GetProperties())
            {
                if (property.PropertyType == typeof(bool) && property.GetValue(record) is bool)
                {
                    var headerAttribute = property.GetCustomAttribute<ExcelHeaderAttribute>();
                    if (headerAttribute != null)
                    {
                        string band = GetBandFromPropertyName(property.Name);
                        string generation = GetGenerationFromPropertyName(property.Name);

                        var generationBandCachekey = (generation, band);
                        if (!generationBandCache.TryGetValue(generationBandCachekey, out var generationBandId))
                        {
                            var generationBand = await _generationBandRepository
                                .Query(gb => !gb.IsDeleted && gb.Generation.Name.Equals(generation) && gb.Band.Name.Equals(band)).FirstOrDefaultAsync(token);
                            if (generationBand != null)
                            {
                                generationBandId = generationBand.Id;
                                generationBandCache[generationBandCachekey] = generationBandId;
                            }
                            else
                            {
                                _logger.LogWarning($"GenerationBand not found: {generation} - {band}");
                                await TerminateProcess(file, token);
                                return;
                            }
                        }
                    }
                }
            }

            Guid CellId = Guid.Empty;
            if (serviceProviderId != Guid.Empty && municipalityId != Guid.Empty && zoneId != Guid.Empty)
            {
                var cellQuery = _cellRepository
                    .Query(c => !c.IsDeleted && c.ServiceProviderId.Equals(serviceProviderId) && c.ZoneId.Equals(zoneId))
                    .Where(c => c.TowerType.Equals(record.TowerType));

                var existingCell = await _cellRepository.FirstOrDefaultAsync(cellQuery, token);
                if (existingCell != null)
                {
                    _logger.LogWarning($"Cell already exists for Service Provider: {record.Provider}, Zone: {record.Zone}");
                    CellId = existingCell.Id;
                }
                else
                {
                    var cell = new Domain.Aggregates.Cell
                    {
                        ServiceProviderId = serviceProviderId,
                        ZoneId = zoneId,
                        FileId = parameters.Id,
                        CoorLatitude = record.Latitude.ToString(),
                        CoorLongitude = record.Longitude.ToString(),
                        TowerType = record.TowerType,
                        //IsFileUploaded = true,
                    };
                    cellList.Add(cell);
                    CellId = cell.Id;
                }
            }
            // Map generation bands for Cellular entity
            var cellularEntries = new List<Domain.Entities.Cellular>();

            // 2G (GSM) Bands
            if (record.Gsm900)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("2G", "900", token);
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.Gsm1800)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("2G", "1800");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }

            // 3G (WCDMA) Bands
            if (record.Wcdma850)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("3G", "850");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.Wcdma2100)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("3G", "2100");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }

            // 4G (FDD-LTE) Bands
            if (record.FddLte2100)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("4G", "2100");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.FddLte900)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("4G", "900");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.FddLte1800)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("4G", "1800");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.FddLte700)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("4G", "700");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.TddLte2300)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("4G", "2300");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.TddLte2500)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("4G", "2500");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.TddLte2600)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("4G", "2600");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }

            // 5G (NR) Bands
            if (record.Nr2600)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("5G", "2600");
                if (generationBand != null)
                {
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
                }
            }
            if (record.Nr3500)
            {
                var generationBand = await _generationBandRepository.GetGenerationBandIdAsync("5G", "3500");
                if (generationBand != null)
                    cellularEntries.Add(new Domain.Entities.Cellular
                    {
                        CellId = CellId,
                        FileId = parameters.Id,
                        ServiceProviderId = serviceProviderId,
                        BarangayId = barangayId,
                        GenerationBandId = generationBand.Id
                    });
            }
            cellularList.AddRange(cellularEntries);
        }

        file.Status = FileStatusEnum.Success;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        if (cellList.Count != 0)
        {
            _cellRepository.AddRange(cellList);
            await _cellRepository.SaveChangesAsync(token);
            _logger.LogInformation("Cell records saved successfully.");
        }
        else
        {
            _logger.LogInformation("No valid cell records found in the Excel file OR cell record already exists.");
        }

        if (cellularList.Count != 0)
        {
            _cellularRepository.AddRange(cellularList);
            await _cellularRepository.SaveChangesAsync(token);
            _logger.LogInformation("Cellular records saved successfully.");
        }
        else
        {
            _logger.LogInformation("No valid cellular records found in the Excel file.");
            await TerminateProcess(file, token);
            return;
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

