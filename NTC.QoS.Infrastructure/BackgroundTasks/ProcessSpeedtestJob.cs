using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Barangay;
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
        var generationCache = new Dictionary<string, Guid>();
        var technologyCache = new Dictionary<string, Guid>();
        var serviceProviderCache = new Dictionary<string, Guid>();
        var cellCache = new Dictionary<string, Guid>();
        var speedtestList = new List<Domain.Entities.Speedtest>();

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

            ReportProgress($"Processing record: {record.Region} {record.Province} {record.Municipality} {record.Barangay}, {record.Timestamp}, {record.NetworkType}, {record.Provider}, ({record.Upload},{record.Download}), {record.RSSI}, {record.SignalQuality}, {record.Provider}, {record.Latitude}, {record.Longitude}, {record.PhoneModel}, {record.CellIdentifier}, {record.MCC}, {record.TAC}");

            var serviceProviderCacheKey = $"{record.Provider}-{ServiceTypeEnum.Wireless}";
            if (!serviceProviderCache.TryGetValue(record.Provider, out var serviceProviderId))
            {
                var serviceProvider = await _serviceProviderRepository.Query(sp => !sp.IsDeleted && sp.Provider.Name.Equals(record.Provider) && sp.Service.Type == ServiceTypeEnum.Wireless).FirstOrDefaultAsync(cancellationToken: token);

                if (serviceProvider is null)
                {
                    _logger.LogWarning("Wireless Service Provider not found: {Provider}", record.Provider);
                    TerminateProcess();
                    return;
                }
                else
                {
                    serviceProviderId = serviceProvider.Id;
                    serviceProviderCache[serviceProviderCacheKey] = serviceProviderId;
                }
            }

            if (!generationCache.TryGetValue(record.NetworkType, out var generationId))
            {
                var generation = await _generationRepository.Query(g => !g.IsDeleted && g.Technologies.Any(t => t.Standard.Equals(record.NetworkType)))
                    .Select(g => new { g.Id })
                    .FirstOrDefaultAsync(cancellationToken: token);

                if (generation is not null)
                {
                    generationId = generation.Id;
                    generationCache[record.NetworkType] = generationId;
                }
                else
                {
                    _logger.LogWarning("Generation not found for Technology: {NetworkType}", record.NetworkType);
                    TerminateProcess();
                    return;
                }
            }


            //Check if all valid Ids are valid
            if (serviceProviderId != Guid.Empty && generationId != Guid.Empty)
            {
                //Create new Speedtest entity
                var speedtest = new Domain.Entities.Speedtest
                {
                    Address = $"{record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}",
                    ServiceProviderId = serviceProviderId,
                    GenerationId = generationId,
                    DateTime = record.Timestamp.ToUniversalTime(),
                    UploadSpeed = record.Upload,
                    DownloadSpeed = record.Download,
                    RSSI = record.RSSI,
                    SignalQuality = EnumHelper<SignalQualityEnum>.ParseUserInput(record.SignalQuality),
                    Location = GeometryHelper.CreatePoint(x: record.Latitude, y: record.Longitude),
                    PhoneModel = record.PhoneModel,
                    CellIdentifier = record.CellIdentifier,
                    MCC = record.MCC,
                    MNC = record.MNC,
                    TAC = record.TAC,
                    Ping = decimal.TryParse(record.Ping, out decimal pingValue) ? pingValue : 0,
                    Jitter = decimal.TryParse(record.Jitter, out decimal jitterValue) ? jitterValue : 0,
                    IsFileUploaded = true,
                };
                speedtestList.Add(speedtest);
            }
        }

        if (speedtestList.Count != 0)
        {
            _SpeedtestRepository.AddRange(speedtestList);
            await _SpeedtestRepository.SaveChangesAsync(token);
            _logger.LogInformation($"Successfully added {speedtestList.Count} speedtest records.");
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