using Hangfire;
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
    private readonly IRepository<Speedtest> _speedtestRepository;
    private readonly IRepository<ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Generation> _generationRepository;
    private readonly IRepository<Domain.Entities.File> _fileRepository;


    public ProcessSpeedtestJob(
        ILogger<ProcessSpeedtestJob> logger,
        IRepository<Speedtest> speedtestRepository,
        IRepository<ServiceProvider> serviceProviderRepository,
        IRepository<Generation> generationRepository,
        IRepository<Domain.Entities.File> fileRepository
        )
    {
        _logger = logger;
        _speedtestRepository = speedtestRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _generationRepository = generationRepository;
        _fileRepository = fileRepository;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        // --- Step 1: Initial File Status Update ---
        var fileQuery = _fileRepository.Query(f => f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file == null)
        {
            const string message = "File not found.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        file.Status = FileStatusEnum.Processing;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        try
        {
            // Step 2: Initialize Collections
            var speedtestList = new List<Speedtest>();
            var speedtestsToAdd = new List<Speedtest>();

            // Step 3: Pre-load Static/Reference Data for Performance
            _logger.LogInformation("Pre-loading reference data.");

            // Pre-load all wireless service providers with optimized query
            var serviceProviderCache = await _serviceProviderRepository
                .Query(sp => sp.Service.Type == ServiceTypeEnum.Wireless)
                .Select(sp => new { sp.Id, sp.Provider.Name })
                .ToDictionaryAsync(
                    sp => CacheKeyGenerator.GenerateServiceProviderCacheKey(sp.Name, ServiceTypeEnum.Wireless),
                    sp => sp.Id,
                    token
                );
            _logger.LogInformation("Pre-loaded {Count} wireless service providers.", serviceProviderCache.Count);

            // Pre-load generations and technologies with optimized query
            var generationCache = await _generationRepository
                .Query(g => !g.IsDeleted)
                .Include(g => g.Technologies)
                .ToListAsync(token);

            var technologyCache = generationCache
                .SelectMany(g => g.Technologies.Select(t => new {
                    t.Standard,
                    TechnologyId = t.Id,
                    GenerationId = g.Id
                }))
                .ToDictionary(
                    x => CacheKeyGenerator.GenerateTechnologyCacheKey(x.Standard),
                    x => new { x.TechnologyId, x.GenerationId }
                );

            _logger.LogInformation("Pre-loaded {Count} generations and technologies.", technologyCache.Count);

            // Pre-load existing speedtest records for duplicate checking
            var providerIds = serviceProviderCache.Values.ToList();
            var existingSpeedtests = await _speedtestRepository
                .Query(s => providerIds.Contains(s.ServiceProviderId))
                .Select(s => new { 
                    s.ServiceProviderId, 
                    s.GenerationId, 
                    s.TechnologyId,
                    s.DateTime, 
                    s.UploadSpeed,
                    s.DownloadSpeed,
                    s.RSSI,
                    s.SignalQuality,
                    s.Location
                })
                .ToListAsync(token);

            var speedtestLookup = new HashSet<string>();
            var duplicateCount = 0;

            foreach (var speedtest in existingSpeedtests)
            {
                // Extract coordinates safely in memory
                var longitude = speedtest.Location?.Coordinate?.X ?? 0;
                var latitude = speedtest.Location?.Coordinate?.Y ?? 0;
                
                // Simplified duplicate key with core fields only 
                var signalQuality = string.IsNullOrEmpty(speedtest.SignalQuality) ? "NULL" : speedtest.SignalQuality;
                var rssi = speedtest.RSSI?.ToString() ?? "NULL";
                var technologyId = speedtest.TechnologyId?.ToString() ?? "NULL";

                var key = $"{speedtest.ServiceProviderId}_{speedtest.GenerationId}_{technologyId}_{speedtest.DateTime:yyyy-MM-dd HH:mm:ss}_{speedtest.UploadSpeed}_{speedtest.DownloadSpeed}_{rssi}_{signalQuality}_{longitude:F6}_{latitude:F6}";
                
                // Debug logging for pre-loaded keys
                _logger.LogDebug("Pre-loaded duplicate key: {DuplicateKey}", key);
                
                if (!speedtestLookup.Add(key))
                {
                    duplicateCount++;
                }
            }

            _logger.LogInformation("Pre-loaded {Count} existing speedtest records ({DuplicateCount} duplicates handled, {TotalSpeedtests} total).",
                speedtestLookup.Count, duplicateCount, existingSpeedtests.Count);

            // Step 4: Process the Excel File
            using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
            _logger.LogInformation("Processing file: {FilePath}", parameters.Path);

            fileStream.Position = 0;
            _logger.LogInformation("Excel processing started");

            var speedtestRecords = ExcelHelper.ParseExcelToClass<SpeedtestRecord>(fileStream);

            if (speedtestRecords.Count is 0)
            {
                _logger.LogInformation("The Excel is empty.");
                file.Status = FileStatusEnum.Success;
            }
            else
            {
                // Step 5: Process Each Record
                var duplicatesSkipped = 0;
                
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

                    var technologyKey = CacheKeyGenerator.GenerateTechnologyCacheKey(record.NetworkType);
                    if (!technologyCache.TryGetValue(technologyKey, out var technologyInfo))
                    {
                        _logger.LogWarning("Technology not found for NetworkType: {NetworkType}. Terminating process.", record.NetworkType);
                        await TerminateProcess(file, token);
                        return;
                    }
                    var technologyId = technologyInfo.TechnologyId;
                    var generationId = technologyInfo.GenerationId;

                    // Check if all valid Ids are valid
                    if (serviceProviderId != Guid.Empty && generationId != Guid.Empty && technologyId != Guid.Empty)
                    {
                        Point location = GeometryHelper.CreatePoint(new Coordinate(y: record.Latitude, x: record.Longitude));

                        var pingValue = decimal.TryParse(record.Ping, out decimal parsedPing) ? parsedPing : 0;
                        var jitterValue = decimal.TryParse(record.Jitter, out decimal parsedJitter) ? parsedJitter : 0;
                        var signalQuality = EnumHelper<SignalQualityEnum>.ParseUserInput(record.SignalQuality).ToString();

                        var duplicateKey = $"{serviceProviderId}_{generationId}_{technologyId}_{record.Timestamp.ToUniversalTime():yyyy-MM-dd HH:mm:ss}_{record.Upload}_{record.Download}_{record.RSSI}_{signalQuality ?? "NULL"}_{record.Longitude:F6}_{record.Latitude:F6}";

                        // Debug logging for duplicate check
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("Generated duplicate key: {DuplicateKey}", duplicateKey);
                        }

                        // Check for duplicates
                        if (!speedtestLookup.Contains(duplicateKey))
                        {
                            // Create new Speedtest entity
                            var speedtest = new Speedtest
                            {
                                Address = $"{record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}",
                                ServiceProviderId = serviceProviderId,
                                GenerationId = generationId,
                                TechnologyId = technologyId,
                                DateTime = record.Timestamp.ToUniversalTime(),
                                UploadSpeed = record.Upload,
                                DownloadSpeed = record.Download,
                                RSSI = record.RSSI,
                                SignalQuality = signalQuality,
                                Location = location,
                                PhoneModel = record.PhoneModel,
                                CellIdentifier = record.CellIdentifier,
                                MCC = record.MCC,
                                MNC = record.MNC,
                                TAC = record.TAC,
                                Ping = pingValue,
                                Jitter = jitterValue,
                                IsFileUploaded = true,
                            };
                            speedtestsToAdd.Add(speedtest);
                            speedtestLookup.Add(duplicateKey);
                        }
                        else
                        {
                            duplicatesSkipped++;
                            _logger.LogInformation("Duplicate speedtest record found and skipped: Provider={Provider}, NetworkType={NetworkType}, DateTime={DateTime}, Location=({Lat},{Lng}), Upload={Upload}, Download={Download}", 
                                record.Provider, record.NetworkType, record.Timestamp, record.Latitude, record.Longitude, record.Upload, record.Download);
                            if (_logger.IsEnabled(LogLevel.Debug))
                            {
                                _logger.LogDebug("Existing duplicate key: {DuplicateKey}", duplicateKey);
                            }
                        }
                    }
                }

                // Step 6: Batch Insert New Records
                if (speedtestsToAdd.Count != 0)
                {
                    _speedtestRepository.AddRange(speedtestsToAdd);
                    await _speedtestRepository.SaveChangesAsync(token);
                    _logger.LogInformation("Successfully added {Count} speedtest records.", speedtestsToAdd.Count);
                }
                else
                {
                    _logger.LogInformation("No valid speedtest records found to add.");
                }

                if (duplicatesSkipped > 0)
                {
                    _logger.LogInformation("Skipped {DuplicateCount} duplicate speedtest records.", duplicatesSkipped);
                }

                file.Status = FileStatusEnum.Success;
            }

            _fileRepository.Update(file);
            await _fileRepository.SaveChangesAsync(token);

            _logger.LogInformation("Processing completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during speedtest processing: {ErrorMessage}", ex.Message);
            await TerminateProcess(file, token);
            throw;
        }
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
        
        _logger.LogError("=== SPEEDTEST PROCESSING TERMINATED DUE TO MISSING DEPENDENCIES ===");
        _logger.LogError("File processing has been terminated because required database entities are missing.");
        _logger.LogError("Please ensure the following entities exist in your database before re-uploading the file:");
        _logger.LogError("1. SERVICE PROVIDERS: All providers mentioned in your Excel file must exist as 'Wireless' service providers");
        _logger.LogError("2. GENERATIONS: All network types (2G, 3G, 4G, 5G) must exist with their corresponding technologies");
        _logger.LogError("3. TECHNOLOGIES: All technologies mentioned in your Excel file must exist in the Technologies table");
        _logger.LogError("======================================================");
    }
}