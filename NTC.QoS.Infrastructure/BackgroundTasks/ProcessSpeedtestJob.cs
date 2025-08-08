using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
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
    private readonly IRepository<Speedtest> _SpeedtestRepository;
    private readonly IRepository<ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Generation> _generationRepository;
    private readonly IRepository<Domain.Entities.File> _fileRepository;


    public ProcessSpeedtestJob(
        ILogger<ProcessSpeedtestJob> logger,
        IRepository<Speedtest> SpeedtestRepository,
        IRepository<ServiceProvider> serviceProviderRepository,
        IRepository<Generation> generationRepository,
        IRepository<Domain.Entities.File> fileRepository
        )
    {
        _logger = logger;
        _SpeedtestRepository = SpeedtestRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _generationRepository = generationRepository;
        _fileRepository = fileRepository;
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {

        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file == null)
        {
            const string message = "File not found";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        file.Status = FileStatusEnum.Processing;

        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        var generationCache = await _generationRepository
            .Query(g => !g.IsDeleted)
            .Include(g => g.Technologies)
            .ToListAsync(token);

        var technologyCache = generationCache
            .SelectMany(g => g.Technologies.Select(t => new { t.Standard, g.Id }))
            .ToDictionary(x => CacheKeyGenerator.GenerateGenerationCacheKey(x.Standard), x => x.Id);

        var serviceProviders = await _serviceProviderRepository
            .Query(sp => !sp.IsDeleted && sp.Service.Type == ServiceTypeEnum.Wireless)
            .Include(sp => sp.Provider)
            .ToListAsync(token);

        var serviceProviderCache = serviceProviders
            .ToDictionary(
                sp => CacheKeyGenerator.GenerateServiceProviderCacheKey(sp.Provider.Name, ServiceTypeEnum.Wireless),
                sp => sp.Id
            );

        var speedtestList = new List<Speedtest>();

        //var generationCache = new Dictionary<string, Guid>();
        //var technologyCache = new Dictionary<string, Guid>();
        //var serviceProviderCache = new Dictionary<string, Guid>();
        //var cellCache = new Dictionary<string, Guid>();
        //var speedtestList = new List<Speedtest>();

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
            ReportProgress($"Processing record: {record.Region}, {record.Provider}, {record.NetworkType}");

            var providerKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wireless);
            if (!serviceProviderCache.TryGetValue(providerKey, out var serviceProviderId))
            {
                _logger.LogWarning("Service Provider not found: {Provider}. Terminating process.", record.Provider);
                await TerminateProcess(file, token);
                return;
            }

            var generationKey = CacheKeyGenerator.GenerateGenerationCacheKey(record.NetworkType);
            if (!technologyCache.TryGetValue(generationKey, out var generationId))
            {
                _logger.LogWarning("Generation not found for NetworkType: {NetworkType}.Terminating process.", record.NetworkType);
                await TerminateProcess(file, token);
                return;
            }

            //Check if all valid Ids are valid
            if (serviceProviderId != Guid.Empty && generationId != Guid.Empty)
            {
                Point location = GeometryHelper.CreatePoint(new Coordinate(y: record.Latitude, x: record.Longitude));

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
                    SignalQuality = EnumHelper<SignalQualityEnum>.ParseUserInput(record.SignalQuality).ToString(),
                    Location = location,
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