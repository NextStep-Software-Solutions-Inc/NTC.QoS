using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.ExcelBindings;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class ProcessNAPFileHandler : IRequestHandler<ProcessNAPFileRequest, bool>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ProcessNAPFileHandler> _logger;
    private readonly IRepository<Domain.Aggregates.NAP> _NAPRepository;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IServiceProviderRepository _serviceProviderRepositoty;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;

    public ProcessNAPFileHandler(
        IBackgroundJobClient backgroundJobClient,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.NAP> NAPRepository,
        IServiceProviderRepository serviceProviderRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        ILogger<ProcessNAPFileHandler> logger
        )
    {
        _backgroundJobClient = backgroundJobClient;
        _fileRepository = fileRepository;
        _serviceProviderRepositoty = serviceProviderRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _NAPRepository = NAPRepository;
        _logger = logger;
    }

    public Task<bool> Handle(ProcessNAPFileRequest request, CancellationToken cancellationToken)
    {

       var processJob = _backgroundJobClient.Enqueue(() => ProcessFile(request.File.Id, cancellationToken));
       var deleteJob = _backgroundJobClient.Schedule(() => DeleteFileAndRecord(request.File.Id, cancellationToken), TimeSpan.FromDays(1));

        return Task.FromResult(true);
    }

    public async Task ProcessFile(Guid id, CancellationToken token)
    {

        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id == id).Select(f => new
        {
            f.Path,
        });
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, cancellationToken: token);



        if (file is null)
        {
            _logger.LogWarning($"File not found: {id}");
            return;
        }

        var serviceProviderCache = new Dictionary<string, Guid>();
        var provinceCache = new Dictionary<string, Guid>();
        var municipalityCache = new Dictionary<string, Guid>();
        var zoneCache = new Dictionary<string, Guid>();
        var NAPList = new List<Domain.Aggregates.NAP>();
        // Implement the logic to process the NAP file here
        // For example, you might want to read the file, validate its contents, and save it to the database
        // Return a success response

        // Perform the processing logic here

        using (var fileStream = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
        {
            // Read the file and process its contents
            // For example, you might want to parse the file and save its contents to the database
            // Return a success response
            _logger.LogInformation($"Processing file: {file.Path}");

            fileStream.Position = 0;
            _logger.LogInformation("Processing started");

            var napRecords = ExcelHelper.ParseExcelToClass<NAPRecord>(fileStream);

            if (napRecords.Count is 0)
            {
                _logger.LogInformation("The Excel is emtpy.");
                return;
            }

            foreach (var record in napRecords)
            {
                // Process each record
                // For example, you might want to validate the record and save it to the database
                _logger.LogInformation($"Processing record: {record}");

                // Check if the Service Provider exists in the cache
                if (!serviceProviderCache.TryGetValue(record.Provider, out var serviceProviderId))
                {
                    // If not, fetch it from the database and add it to the cache
                    var serviceProviderQuery = _serviceProviderRepositoty.Query(sp => !sp.IsDeleted);
                    var serviceProviderNameQuery = serviceProviderQuery.Select(sp => new
                    {
                        sp.Id,
                        ProviderName = sp.Provider.Name,
                        ServiceName = sp.Service.Name,
                    });
                    var serviceProvider = await _serviceProviderRepositoty.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken: token);

                    if (serviceProvider is null)
                    {
                        _logger.LogWarning($"Service Provider not found: {record.Provider}");
                        continue; // Skip this record
                    }
                    else
                    {
                        serviceProviderId = serviceProvider.Id;
                        serviceProviderCache[record.Provider] = serviceProviderId;
                    }
                }
                // Check if the Province exists in the cache
                if (!provinceCache.TryGetValue(record.Province, out var provinceId))
                {
                    // If not, fetch it from the database and add it to the cache
                    var provinceQuery = _provinceRepository.Query(p => !p.IsDeleted);
                    var provinceNameQuery = provinceQuery.Select(p => new
                    {
                        p.Id,
                        p.Name
                    });
                    var province = await _provinceRepository.FirstOrDefaultAsync(provinceNameQuery, cancellationToken: token);
                    if (province is null)
                    {
                        _logger.LogWarning($"Province not found: {record.Province}");
                        continue; // Skip this record
                    }
                    else
                    {
                        provinceId = province.Id;
                        provinceCache[record.Province] = provinceId;
                    }
                }

                // Check if the Municipality exists in the cache
                if (!municipalityCache.TryGetValue(record.Municipality, out var municipalityId))
                {
                    // If not, fetch it from the database and add it to the cache
                    var municipalityQuery = _municipalityRepository.Query(m => !m.IsDeleted);
                    var municipalityNameQuery = municipalityQuery
                        .Where(m => m.ProvinceId == provinceId)
                        .Select(m => new
                        {
                            m.Id,
                            m.Name,
                            Barangays = m.Barangays.Select(b => new
                            {
                                b.Id,
                                b.Name,
                                Zones = b.Zones.Select(z => new
                                {
                                    z.Id,
                                    z.Name
                                })
                            })
                        });
                    var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityNameQuery, cancellationToken: token);
                    if (municipality is null)
                    {
                        _logger.LogWarning($"Municipality under {record.Province} not found: {record.Municipality}");
                        continue; // Skip this record
                    }
                    else
                    {
                        municipalityId = municipality.Id;
                        municipalityCache[record.Municipality] = municipalityId;

                        // get the first zone id from the barangay of the municipality
                        // and add it to the zoneCache

                        var zone = municipality.Barangays
                            .SelectMany(b => b.Zones)
                            .FirstOrDefault();

                        if (zone is null)
                        {
                            _logger.LogWarning($"No zones found for municipality: {record.Municipality}");
                            continue; // Skip this record
                        }
                        else
                        {
                            zoneCache[record.Municipality] = zone.Id;
                        }
                    }
                }

                // Check if all Id is present
                if (serviceProviderId == Guid.Empty && municipalityId == Guid.Empty && (!zoneCache.TryGetValue(record.Municipality, out var zoneId) || zoneId == Guid.Empty))
                {
                    // Create a new NAP entity
                    var nap = new Domain.Aggregates.NAP
                    {
                        ServiceProviderId = serviceProviderId,
                        ZoneId = zoneId,
                        FileId = id,
                        CoorLatitude = record.Latitude.ToString(),
                        CoorLongitude = record.Longitude.ToString(),
                    };

                    NAPList.Add(nap);
                }
               
            }
            // Add the NAP entities to the repository
            _NAPRepository.AddRange(NAPList);

            await _NAPRepository.SaveChangesAsync(token);

            _logger.LogInformation("Processing completed");

        }

    }
    public async Task DeleteFileAndRecord(Guid fileId, CancellationToken token)
    {

        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id == fileId);

        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, cancellationToken: token);
        if (file is null)
        {
            _logger.LogWarning($"File not found: {fileId}");
            return;
        }
        // Delete the file from the file system
        if (System.IO.File.Exists(file.Path))
        {
            System.IO.File.Delete(file.Path);
            _logger.LogInformation($"File deleted: {file.Path}");
        }
        else
        {
            _logger.LogWarning($"File not found on disk: {file.Path}");
        }
        // Delete the file record from the database
        _fileRepository.Delete(file);
        await _fileRepository.SaveChangesAsync(token);
        _logger.LogInformation($"File record deleted: {file.Id}");

    }
}

