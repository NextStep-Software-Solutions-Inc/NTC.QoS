using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessSpeedtestJob : IProcessSpeedtestJob
{
    private readonly ILogger<ProcessSpeedtestJob> _logger;
    private readonly IRepository<Domain.Entities.Speedtest> _SpeedtestRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository; 
    private readonly IRepository<Domain.Entities.Region> _regionRepository;
    private readonly IRepository<Domain.Entities.Technology> _technologyRepository;
    private readonly IRepository<Domain.Aggregates.Generation> _generationRepository;

    public ProcessSpeedtestJob(
        ILogger<ProcessSpeedtestJob> logger,
        IRepository<Domain.Entities.Speedtest> SpeedtestRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Region> regionRepository,
        IRepository<Domain.Entities.Technology> technologyRepository,
        IRepository<Domain.Aggregates.Generation> generationRepository
        )
    {
        _logger = logger;
        _SpeedtestRepository = SpeedtestRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _regionRepository = regionRepository;
        _generationRepository = generationRepository;
        _technologyRepository = technologyRepository;
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var regionCache = new Dictionary<string, Guid>();
        var provinceCache = new Dictionary<string, Guid>();
        var municipalityCache = new Dictionary<string, Guid>();
        var barangayCache = new Dictionary<string, Guid>();
        var generationCache = new Dictionary<string, Guid>();
        var serviceProviderCache = new Dictionary<string, Guid>();
        var SpeedtestList = new List<Domain.Entities.Speedtest>();

        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        _logger.LogInformation($"Processing file: {parameters.Path}");

        fileStream.Position = 0;
        _logger.LogInformation($"Processing started");

        var speedtestRecords = ExcelHelper.ParseExcelToClass<SpeedtestRecord>(fileStream);

        if (speedtestRecords.Count is 0)
        {
            _logger.LogInformation("The Excel is empty.");
        }

        foreach (var record in speedtestRecords)
        {
            //Process each record
            _logger.LogInformation($"Processing record: {record}");

            ReportProgress($"Processing record: {record.Region} {record.Province} {record.City} {record.Barangay}, {record.Timestamp}, {record.Generation}, {record.Provider}, ({record.Upload},{record.Download}), {record.RSSI}, {record.Remarks}");

            // Check if the Region exists in the cache
            if (!regionCache.TryGetValue(record.Region, out var regionId))
            {
                //If not, fetch it from the database and add it to the cache
                var regionQuery = _regionRepository
                    .Query(r => !r.IsDeleted)
                    .Where(r => r.Name.Equals(record.Region));

                var regionNameQuery = regionQuery.Select(r => new
                {
                    r.Id,
                    r.Name
                });

                var region = await _regionRepository.FirstOrDefaultAsync(regionNameQuery, cancellationToken: token);
                if (region is null)
                {
                    _logger.LogWarning($"Region not found: {record.Region}");
                    TerminateProcess();
                    return;
                }
                else
                {
                    regionId = region.Id;
                    regionCache[record.Region] = regionId;
                }
            }

            // Check if the Region exists in the cache
            if (!provinceCache.TryGetValue(record.Province, out var provinceId))
            {
                //If not, fetch it from the database and add it to the cache
                var provinceQuery = _provinceRepository
                    .Query(p => !p.IsDeleted)
                    .Where(p => p.Name.Equals(record.Province));

                var provinceNameQuery = provinceQuery.Select(p => new
                {
                    p.Id,
                    p.Name
                });

                var province = await _provinceRepository.FirstOrDefaultAsync(provinceNameQuery, cancellationToken: token);
                if (province is null)
                {
                    _logger.LogWarning($"Province not found: {record.Province}");
                    TerminateProcess();
                    return;
                }
                else
                {
                    provinceId = province.Id;
                    provinceCache[record.Province] = provinceId;
                }
            }

            // Check if the Municipality exists in the cache
            if (!municipalityCache.TryGetValue(record.City, out var municipalityId))
            {
                // If not, fetch it from the database and add it to the cache
                var municipalityQuery = _municipalityRepository.Query(m => !m.IsDeleted);
                var municipalityNameQuery = municipalityQuery
                    .Where(m => m.ProvinceId == provinceId)
                    .Where(m => m.Name.Equals(record.City))
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        Barangays = m.Barangays.Select(b => new
                        {
                            b.Id,
                            b.Name
                        })
                    });
                var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityNameQuery, cancellationToken: token);
                if (municipality is null)
                {
                    _logger.LogWarning($"Municipality under {record.Province} not found: {record.City}");
                    TerminateProcess();
                    return;
                }
                else
                {
                    municipalityId = municipality.Id;
                    municipalityCache[record.City] = municipalityId;
                }
            }

            // Check if the Barangay exists in the cache
            if (!barangayCache.TryGetValue(record.Barangay, out var barangayId))
            {
                // If not, fetch it from the database and add it to the cache
                var barangayQuery = _barangayRepository
                    .Query(p => !p.IsDeleted)
                    .Where(p => p.Name.Equals(record.Barangay));

                var barangayNameQuery = barangayQuery.Select(p => new
                {
                    p.Id,
                    p.Name
                });
                var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayNameQuery, cancellationToken: token);
                if (barangay is null)
                {
                    _logger.LogWarning($"Barangay not found: {record.Barangay}");
                    TerminateProcess();
                    return;
                }
                else
                {
                    barangayId = barangay.Id;
                    barangayCache[record.Barangay] = barangayId;
                }
            }

            if (!serviceProviderCache.TryGetValue(record.Provider, out var serviceProviderId))
            {
                // If not, fetch it from the database and add it to the cache
                var serviceProviderQuery = _serviceProviderRepository
                    .Query(sp => !sp.IsDeleted)
                    .Where(sp => sp.Provider.Name.Equals(record.Provider));

                var serviceProviderNameQuery = serviceProviderQuery.Select(sp => new
                {
                    sp.Id,
                    ProviderName = sp.Provider.Name,
                    ServiceName = sp.Service.Name,
                });

                var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderNameQuery, cancellationToken: token);

                if (serviceProvider is null)
                {
                    _logger.LogWarning($"Service Provider not found: {record.Provider}");
                    TerminateProcess();
                    return;

                }
                else
                {
                    serviceProviderId = serviceProvider.Id;
                    serviceProviderCache[record.Provider] = serviceProviderId;
                }
            }

            // Check if the Generation exists in the cache
            if (!generationCache.TryGetValue(record.Generation, out var generationId))
            {
                // If not, fetch it from the database and add it to the cache
                var generationQuery = _generationRepository
                    .Query(g => !g.IsDeleted)
                    .Where(g => g.Name.Equals(record.Generation));

                var generationNameQuery = generationQuery.Select(g => new
                {
                    g.Id,
                    g.Name
                });
                var generation = await _generationRepository.FirstOrDefaultAsync(generationNameQuery, cancellationToken: token);
                if (generation is null)
                {
                    _logger.LogWarning($"Generation not found: {record.Generation}");
                    TerminateProcess();
                    return;
                }
                else
                {
                    generationId = generation.Id;
                    generationCache[record.Generation] = generationId;
                }
            }

            DateTime utcDateTime = record.Timestamp.ToUniversalTime();

            //Check if all valid Ids are valid
            if (barangayId != Guid.Empty && serviceProviderId != Guid.Empty && generationId != Guid.Empty)
            {
                //Create new Speedtest entity
                var speedtest = new Domain.Entities.Speedtest
                {
                    BarangayId = barangayId,
                    ServiceProviderId = serviceProviderId,
                    GenerationId = generationId,
                    DateTime = record.Timestamp,
                    UploadSpeed = record.Upload,
                    DownloadSpeed = record.Download,
                    RSSI = decimal.TryParse(record.RSSI, out var rssiValue) ? rssiValue : 0,
                    SignalQuality = EnumHelper<SignalQualityEnum>.ParseUserInput(record.Remarks)
                };
                SpeedtestList.Add(speedtest);
            }
        }


        if (SpeedtestList.Count != 0)
        {
            _SpeedtestRepository.AddRange(SpeedtestList);
            await _SpeedtestRepository.SaveChangesAsync(token);
            _logger.LogInformation($"Successfully added {SpeedtestList.Count} speedtest records.");
        }
        else
        {
            _logger.LogInformation("No valid speedtest records found to add.");
        }

        _logger.LogInformation("Processing completed");
        }

        void ReportProgress(string message)
        {
            _logger.LogInformation(message);
        }

        private void TerminateProcess()
        {
            _logger.LogInformation("Process terminated due to error in runtime");
        }
}