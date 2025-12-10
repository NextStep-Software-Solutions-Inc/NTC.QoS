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
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserContextService _userContextService;

    private const int BatchSize = 1000; // Process records in batches
    private const int DuplicateLoadBatchSize = 10000; // Load existing records in batches for memory efficiency

    public ProcessSpeedtestJob(
        ILogger<ProcessSpeedtestJob> logger,
        IRepository<Speedtest> speedtestRepository,
        IRepository<ServiceProvider> serviceProviderRepository,
        IRepository<Generation> generationRepository,
        IRepository<Domain.Entities.File> fileRepository,
        IBlobStorageService blobStorageService,
        IUserContextService userContextService
        )
    {
        _logger = logger;
        _speedtestRepository = speedtestRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _generationRepository = generationRepository;
        _fileRepository = fileRepository;
        _blobStorageService = blobStorageService;
        _userContextService = userContextService;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Get user context once for all batches
        var userId = _userContextService.GetCurrentUserId() ?? "System";
        var now = DateTime.UtcNow;

        // --- Step 1: Initial File Status Update ---
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(
            parameters.Id, _fileRepository, _logger, FileTypeEnum.Speedtest, token);

        await ProcessingJobHelper.SetFileProcessingStatusAsync(
            file, _fileRepository, _logger, FileTypeEnum.Speedtest, token);

        try
        {
            // Step 2: Pre-load Static/Reference Data for Performance
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
                .SelectMany(g => g.Technologies.Select(t => new
                {
                    t.Standard,
                    TechnologyId = t.Id,
                    GenerationId = g.Id
                }))
                .ToDictionary(
                    x => CacheKeyGenerator.GenerateTechnologyCacheKey(x.Standard),
                    x => new { x.TechnologyId, x.GenerationId }
                );

            _logger.LogInformation("Pre-loaded {Count} generations and technologies.", technologyCache.Count);

            // OPTIMIZED: Pre-load existing speedtest records for duplicate checking
            var providerIds = serviceProviderCache.Values.ToList();

            // OPTIMIZED: Use hash-based lookup instead of string keys
            var speedtestLookup = new HashSet<int>();
            var duplicateCount = 0;
            var totalLoadedRecords = 0;

            // Get total count first
            var totalSpeedtests = await _speedtestRepository
                .Query(s => providerIds.Contains(s.ServiceProviderId))
                .CountAsync(token);

            _logger.LogInformation("Loading {TotalCount} existing speedtest records in batches of {BatchSize}...",
                totalSpeedtests, DuplicateLoadBatchSize);

            // Load existing speedtests in batches to avoid memory overload
            var skip = 0;
            while (true)
            {
                var existingBatch = await _speedtestRepository
                    .Query(s => providerIds.Contains(s.ServiceProviderId))
                    .OrderBy(s => s.Id)
                    .Skip(skip) // Skip records we already processed
                    .Take(DuplicateLoadBatchSize)
                    .Select(s => new
                    {
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

                if (existingBatch.Count == 0)
                    break; // No more records, exit loop

                foreach (var speedtest in existingBatch)
                {
                    var longitude = speedtest.Location?.Coordinate?.X ?? 0;
                    var latitude = speedtest.Location?.Coordinate?.Y ?? 0;

                    var hash = GenerateSpeedtestHash(
                        speedtest.ServiceProviderId,
                        speedtest.GenerationId ?? Guid.Empty,
                        speedtest.TechnologyId ?? Guid.Empty,
                        speedtest.DateTime,
                        speedtest.UploadSpeed,
                        speedtest.DownloadSpeed,
                        speedtest.RSSI ?? 0,
                        speedtest.SignalQuality ?? "NULL",
                        longitude,
                        latitude
                    );

                    if (!speedtestLookup.Add(hash))
                    {
                        duplicateCount++;
                    }
                }
                totalLoadedRecords += existingBatch.Count;
                skip += DuplicateLoadBatchSize; // Move to next batch

                _logger.LogInformation("Loaded {LoadedCount}/{TotalCount} existing records...",
                    totalLoadedRecords, totalSpeedtests);
            }

            _logger.LogInformation("Pre-loaded {Count} unique speedtest records ({DuplicateCount} duplicates handled in existing data).",
                speedtestLookup.Count, duplicateCount);

            // Step 3: Download file from blob storage or access local file
            Stream fileStream;
            try
            {
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(
                    parameters.Path, _blobStorageService, _logger, FileTypeEnum.Speedtest, token);
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.Speedtest, token);
                return;
            }

            // Step 4: Process the Excel File
            using (fileStream)
            {
                _logger.LogInformation("Processing Speedtest file downloaded from blob storage");

                fileStream.Position = 0;
                _logger.LogInformation("Excel processing started");

                var speedtestRecords = ExcelHelper.ParseExcelToClass<SpeedtestRecord>(fileStream);

                if (speedtestRecords.Count is 0)
                {
                    _logger.LogInformation("The Excel is empty.");
                    await ProcessingJobHelper.SetFileSuccessStatusAsync(
                        file, _fileRepository, _logger, FileTypeEnum.Speedtest, token);
                    return;
                }

                // Step 5: Process Records in Batches (OPTIMIZED)
                var totalRecords = speedtestRecords.Count;
                var totalAdded = 0;
                var totalDuplicatesSkipped = 0;

                for (int i = 0; i < totalRecords; i += BatchSize)
                {
                    var batch = speedtestRecords.Skip(i).Take(BatchSize).ToList();
                    var batchNumber = (i / BatchSize) + 1;
                    var totalBatches = (int)Math.Ceiling((double)totalRecords / BatchSize);

                    _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({RecordCount} records)",
                        batchNumber, totalBatches, batch.Count);

                    var speedtestsToAdd = new List<Speedtest>();
                    var duplicatesSkipped = 0;

                    foreach (var record in batch)
                    {
                        ProcessingJobHelper.ReportProgress($"Processing record: {record.Region}, {record.Provider}, {record.NetworkType}", _logger);

                        var providerKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wireless);
                        if (!serviceProviderCache.TryGetValue(providerKey, out var serviceProviderId))
                        {
                            _logger.LogWarning("Service Provider not found: {Provider}. Terminating process.", record.Provider);
                            await ProcessingJobHelper.TerminateProcessAsync(
                                file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                                $"Service Provider '{record.Provider}' not found in database", token);
                            return;
                        }

                        var technologyKey = CacheKeyGenerator.GenerateTechnologyCacheKey(record.NetworkType);
                        if (!technologyCache.TryGetValue(technologyKey, out var technologyInfo))
                        {
                            _logger.LogWarning("Technology not found for NetworkType: {NetworkType}. Terminating process.", record.NetworkType);
                            await ProcessingJobHelper.TerminateProcessAsync(
                                file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                                $"Technology '{record.NetworkType}' not found in database", token);
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

                            // Generate hash for duplicate check (OPTIMIZED)
                            var duplicateHash = GenerateSpeedtestHash(
                                serviceProviderId,
                                generationId,
                                technologyId,
                                record.Timestamp.ToUniversalTime(),
                                record.Upload,
                                record.Download,
                                record.RSSI ?? 0,  // Handle nullable RSSI
                                signalQuality ?? "NULL",
                                record.Longitude,
                                record.Latitude
                            );

                            // Check for duplicates
                            if (!speedtestLookup.Contains(duplicateHash))
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
                                    CreatedAt = now,
                                    CreatedBy = userId,
                                    UpdatedAt = now,
                                    UpdatedBy = userId
                                };
                                speedtestsToAdd.Add(speedtest);
                                speedtestLookup.Add(duplicateHash);
                            }
                            else
                            {
                                duplicatesSkipped++;
                                if (_logger.IsEnabled(LogLevel.Debug))
                                {
                                    _logger.LogDebug("Duplicate speedtest record found and skipped: Provider={Provider}, NetworkType={NetworkType}, DateTime={DateTime}, Location=({Lat},{Lng}), Upload={Upload}, Download={Download}",
                                        record.Provider, record.NetworkType, record.Timestamp, record.Latitude, record.Longitude, record.Upload, record.Download);
                                }
                            }
                        }
                    }

                    // OPTIMIZED: Save batch (saves periodically instead of all at once)
                    if (speedtestsToAdd.Count > 0)
                    {
                        _speedtestRepository.AddRange(speedtestsToAdd);
                        await _speedtestRepository.SaveChangesAsync(token);
                        totalAdded += speedtestsToAdd.Count;
                        _logger.LogInformation("Batch {BatchNumber}/{TotalBatches}: Added {Count} records, skipped {DuplicateCount} duplicates.",
                            batchNumber, totalBatches, speedtestsToAdd.Count, duplicatesSkipped);
                    }
                    else
                    {
                        _logger.LogInformation("Batch {BatchNumber}/{TotalBatches}: No new records to add, skipped {DuplicateCount} duplicates.",
                            batchNumber, totalBatches, duplicatesSkipped);
                    }

                    totalDuplicatesSkipped += duplicatesSkipped;

                    // Optional: Report progress for Hangfire
                    ProcessingJobHelper.ReportProgress($"Processed {Math.Min(i + BatchSize, totalRecords)}/{totalRecords} records", _logger);
                }

                _logger.LogInformation("Processing completed: Added {TotalAdded} records, skipped {TotalDuplicates} duplicates out of {TotalRecords} total records.",
                    totalAdded, totalDuplicatesSkipped, totalRecords);

                await ProcessingJobHelper.SetFileSuccessStatusAsync(
                    file, _fileRepository, _logger, FileTypeEnum.Speedtest, token);

                stopwatch.Stop();
                _logger.LogInformation("Processing completed successfully in {ElapsedTime} ({ElapsedMilliseconds}ms).",
                    stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"), stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error occurred during speedtest processing after {ElapsedTime}: {ErrorMessage}",
                stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"), ex.Message);
            await ProcessingJobHelper.TerminateProcessAsync(
                file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                $"Error occurred during processing: {ex.Message}", token);
            throw;
        }
    }

    /// <summary>
    /// Generates a hash code for speedtest duplicate detection
    /// Uses hash-based comparison instead of string concatenation for better memory efficiency
    /// </summary>
    private static int GenerateSpeedtestHash(
        Guid serviceProviderId,
        Guid generationId,
        Guid technologyId,
        DateTime dateTime,
        decimal uploadSpeed,
        decimal downloadSpeed,
        decimal rssi,
        string signalQuality,
        double longitude,
        double latitude)
    {
        var hash = new HashCode();
        hash.Add(serviceProviderId);
        hash.Add(generationId);
        hash.Add(technologyId);
        hash.Add(dateTime);
        hash.Add(uploadSpeed);
        hash.Add(downloadSpeed);
        hash.Add(rssi);
        hash.Add(signalQuality);
        hash.Add(Math.Round(longitude, 6));
        hash.Add(Math.Round(latitude, 6));
        return hash.ToHashCode();
    }
}