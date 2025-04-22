using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.ExcelBindings;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System.IO;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class ProcessNAPFileHandler : IRequestHandler<ProcessNAPFileRequest, bool>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ProcessNAPFileHandler> _logger;
    private readonly IRepository<Domain.Aggregates.NAP> _NAPRepository;
    private readonly IServiceProviderRepository _serviceProviderRepositoty;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;

    public ProcessNAPFileHandler(
        IBackgroundJobClient backgroundJobClient,
        IFileRepository fileRepository,
        IRepository<Domain.Aggregates.NAP> NAPRepository,
        IServiceProviderRepository serviceProviderRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        ILogger<ProcessNAPFileHandler> logger
        )
    {
        _backgroundJobClient = backgroundJobClient;
        _serviceProviderRepositoty = serviceProviderRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _NAPRepository = NAPRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessNAPFileRequest request, CancellationToken cancellationToken)
    {
        //store the file temporarily to be accessd without serialization of json



        using (var stream = new MemoryStream())
        {
            await request.File.File.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            _logger.LogInformation("Processing started");

            var napRecords = ExcelHelper.ParseExcelToClass<NAPRecord>(stream);

            if (napRecords.Count is 0)
            {
                _logger.LogInformation("The Excel is emtpy.");
            }
            _backgroundJobClient.Enqueue(() => ProcessFile(stream, cancellationToken));

        }



        // Enqueue the background job

        return true;
    }

    public async Task ProcessFile(Stream strem, CancellationToken token)
    {

        var serviceProviderCache = new Dictionary<string, Guid>();
        var provinceCache = new Dictionary<string, Guid>();
        var municipalityCache = new Dictionary<string, Guid>();
        var zoneCache = new Dictionary<string, Guid>();
        // Implement the logic to process the NAP file here
        // For example, you might want to read the file, validate its contents, and save it to the database
        // Return a success response

        // Perform the processing logic here

        using (var stream = new MemoryStream())
        {
            //await request.File.File.CopyToAsync(stream, token);
            stream.Position = 0;
            _logger.LogInformation("Processing started");

            var napRecords = ExcelHelper.ParseExcelToClass<NAPRecord>(stream);

            if(napRecords.Count is 0)
            {
                _logger.LogInformation("The Excel is emtpy.");
                return;
            }

            foreach(var record in napRecords)
            {
                // Process each record
                // For example, you might want to validate the record and save it to the database
                _logger.LogInformation($"Processing record: {record}");

                // Check if the Service Provider exists in the cache
                //if (!serviceProviderCache.TryGetValue(record.Provider, out var serviceProviderId))
                //{
                //    // If not, fetch it from the database and add it to the cache
                //    var serviceProviderQuery = _serviceProviderRepositoty.Query(sp => !sp.IsDeleted);
                //    var serviceProviderNameQuery = serviceProviderQuery.Select(sp => new
                //    {
                //        sp.Id,
                //        ProviderName = sp.Provider.Name,
                //        ServiceName = sp.Service.Name,
                //    });
                //    var serviceProvider = await _serviceProviderRepositoty.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken: token);

                //    if (serviceProvider is null)
                //    {
                //        _logger.LogWarning($"Service Provider not found: {record.Provider}");
                //        continue; // Skip this record
                //    }
                //    else
                //    {
                //        serviceProviderId = serviceProvider.Id;
                //        serviceProviderCache[record.Provider] = serviceProviderId;
                //    }
                //}
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

                        // get the firts zone id from the barangay of the municipality
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
                            var zoneId = zone.Id;
                            zoneCache[record.Municipality] = zoneId;
                        }
                    }

                    // Create a new NAP entity
                    var nap = new Domain.Aggregates.NAP
                    {
                        //ServiceProviderId = serviceProviderId,
                        ZoneId = zoneCache[record.Municipality],
                        CoorLatitude = record.Latitude.ToString(),
                        CoorLongitude = record.Longitude.ToString(),
                    };

                    // Add the NAP entity to the repository
                    _NAPRepository.Add(nap);
                    await _NAPRepository.SaveChangesAsync(token);
                }
            }

        }

        _logger.LogInformation("Processing completed");

    }

}

