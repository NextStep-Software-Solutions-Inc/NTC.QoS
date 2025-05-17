using System.Globalization;
using System.Threading;
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
using NTC.QoS.Infrastructure.Persistence.Repositories;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessSpeedtestJob : IProcessSpeedtestJob
{
    private readonly ILogger<ProcessSpeedtestJob> _logger;
    private readonly IRepository<Speedtest> _SpeedtestRepository;
    private readonly IRepository<ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Province> _provinceRepository;
    private readonly IRepository<Municipality> _municipalityRepository;
    private readonly IRepository<Barangay> _barangayRepository;
    private readonly IRepository<Region> _regionRepository;
    private readonly IRepository<Technology> _technologyRepository;
    private readonly IRepository<Generation> _generationRepository;
    private readonly IRepository<Domain.Entities.File> _fileRepository;


    public ProcessSpeedtestJob(
        ILogger<ProcessSpeedtestJob> logger,
        IRepository<Speedtest> SpeedtestRepository,
        IRepository<ServiceProvider> serviceProviderRepository,
        IRepository<Province> provinceRepository,
        IRepository<Municipality> municipalityRepository,
        IRepository<Barangay> barangayRepository,
        IRepository<Region> regionRepository,
        IRepository<Technology> technologyRepository,
        IRepository<Generation> generationRepository,
        IRepository<Domain.Entities.File> fileRepository
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
        _fileRepository = fileRepository;
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
        }
        ;

        file.Status = FileStatusEnum.Processing;

        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        var generationCache = new Dictionary<string, Guid>();
        var technologyCache = new Dictionary<string, Guid>();
        var serviceProviderCache = new Dictionary<string, Guid>();
        var cellCache = new Dictionary<string, Guid>();
        var speedtestList = new List<Speedtest>();

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
                    await TerminateProcess(file, token);
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
                    await TerminateProcess(file, token);
                    return;
                }
            }


            //Check if all valid Ids are valid
            if (serviceProviderId != Guid.Empty && generationId != Guid.Empty)
            {
                //Create new Speedtest entity
                var speedtest = new Speedtest
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

        file.Status = FileStatusEnum.Success;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        _logger.LogInformation("Processing completed");
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
}