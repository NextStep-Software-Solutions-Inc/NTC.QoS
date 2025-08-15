using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessNAPJob : IProcessNAPJob
{
    private readonly ILogger<ProcessNAPJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.NAP> _NAPRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;

    public ProcessNAPJob(
        ILogger<ProcessNAPJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.NAP> NAPRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _NAPRepository = NAPRepository;
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

        var serviceProviderCache = new Dictionary<string, Guid>();
        var NAPList = new List<Domain.Aggregates.NAP>();

        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        _logger.LogInformation($"Processing file: {parameters.Path}");

        fileStream.Position = 0;
        _logger.LogInformation("Processing started");

        var napRecords = ExcelHelper.ParseExcelToClass<NAPRecord>(fileStream);

        if (napRecords.Count is 0)
        {
            _logger.LogInformation("The Excel is empty.");
        }

        // Preload existing NAPs for duplicate checking
        var existingNAPs = await _NAPRepository
            .ToListAsync(_NAPRepository.QueryAsNoTracking(nap => !nap.IsDeleted), token);

        foreach (var record in napRecords)
        {
            _logger.LogInformation($"Processing record: {record}");

            ReportProgress($"Processing record: {record.Provider}, {record.Municipality} {record.Province} ({record.Latitude},{record.Longitude})");

            if (!serviceProviderCache.TryGetValue(record.Provider, out var serviceProviderId))
            {
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
                    await TerminateProcess(file, token);
                    return;
                }
                else
                {
                    serviceProviderId = serviceProvider.Id;
                    serviceProviderCache[record.Provider] = serviceProviderId;
                }
            }

            if (serviceProviderId != Guid.Empty)
            {
                double latitude, longitude;
                try
                {
                    latitude = record.Latitude;
                    longitude = record.Longitude;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Invalid latitude/longitude for record: {record.Provider}, {record.Municipality} {record.Province}. Error: {ex.Message}");
                    continue;
                }

                var address = $"{record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";
                var location = GeometryHelper.CreatePoint(new NetTopologySuite.Geometries.Coordinate(y: latitude, x: longitude));

                // Check for duplicate: ServiceProviderId + Address + Location
                bool isDuplicate = existingNAPs.Any(n =>
                    n.ServiceProviderId == serviceProviderId &&
                    n.Address.Equals(address, StringComparison.OrdinalIgnoreCase) &&
                    n.Location.X == location.X &&
                    n.Location.Y == location.Y);

                if (isDuplicate)
                {
                    _logger.LogInformation($"Duplicate NAP found for Provider: {record.Provider}, Address: {address}, Location: ({latitude},{longitude}). Skipping.");
                    continue;
                }

                var nap = new Domain.Aggregates.NAP
                {
                    ServiceProviderId = serviceProviderId,
                    Address = address,
                    Location = location,
                    IsFileUploaded = true,
                };
                NAPList.Add(nap);
            }
        }

        if (NAPList.Count > 0)
        {
            _NAPRepository.AddRange(NAPList);
            await _NAPRepository.SaveChangesAsync(token);
            _logger.LogInformation($"Added {NAPList.Count} new NAP records.");
        }
        else
        {
            _logger.LogInformation("No new NAP records to add.");
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
        _logger.LogInformation("Process terminated due to error in runtime.");
    }
}

