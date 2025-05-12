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
        
        if(file == null)
        {
            var message = "File not found";
            _logger.LogError(message);
            throw new Exception(message);
        };

        file.Status = FileStatusEnum.Processing;

        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        var serviceProviderCache = new Dictionary<string, Guid>();
        var provinceCache = new Dictionary<string, Guid>();
        var municipalityCache = new Dictionary<string, Guid>();
        var zoneCache = new Dictionary<string, Guid>();
        var NAPList = new List<Domain.Aggregates.NAP>();
        // Implement the logic to process the NAP file here
        // For example, you might want to read the file, validate its contents, and save it to the database
        // Return a success response

        // Perform the processing logic here

        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        // Read the file and process its contents
        // For example, you might want to parse the file and save its contents to the database
        // Return a success response
        _logger.LogInformation($"Processing file: {parameters.Path}");

        fileStream.Position = 0;
        _logger.LogInformation("Processing started");

        var napRecords = ExcelHelper.ParseExcelToClass<NAPRecord>(fileStream);

        if (napRecords.Count is 0)
        {
            _logger.LogInformation("The Excel is empty.");
        }

        foreach (var record in napRecords)
        {
            // Process each record
            _logger.LogInformation($"Processing record: {record}");

            ReportProgress($"Processing record: {record.Provider}, {record.Municipality} {record.Province} ({record.Latitude},{record.Longitude})");

            // Check if the Service Provider exists in the cache
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
                    await TerminateProcess(file, token);
                    return;

                }
                else
                {
                    serviceProviderId = serviceProvider.Id;
                    serviceProviderCache[record.Provider] = serviceProviderId;
                }
            }

            // Check if all Ids are valid and present
            if (serviceProviderId != Guid.Empty)
            {
                Point location = GeometryHelper.CreatePoint(x: record.Latitude, y: record.Longitude);
                // Create a new NAP entity
                var nap = new Domain.Aggregates.NAP
                {
                    ServiceProviderId = serviceProviderId,
                    Address = $"{record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}",
                    Location = location,
                    IsFileUploaded = true,
                };
                NAPList.Add(nap);
            }

        }
        // Add the NAP entities to the repository
        _NAPRepository.AddRange(NAPList);

        file.Status = FileStatusEnum.Success;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);
        await _NAPRepository.SaveChangesAsync(token);

        _logger.LogInformation("Processing completed");

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
}

