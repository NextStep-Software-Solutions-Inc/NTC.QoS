using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;
using System.Diagnostics;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessNAPJob : IProcessNAPJob
{
    private readonly ILogger<ProcessNAPJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.NAP> _NAPRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserContextService _userContextService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IGenerateConnectivityAssessmentJob _generateConnectivityAssessmentJob;
    private readonly IMemoryCache _memoryCache;

    private const string NAPsCacheKeysSet = "naps_cache_keys";

    // Batch processing configuration (following ProcessCellJobBatched pattern)
    private const int BATCH_SIZE = 5000; // Large batch size for simple entity
    private const int DUPLICATE_LOAD_BATCH_SIZE = 10000; // Load existing records in batches for memory efficiency

    public ProcessNAPJob(
        ILogger<ProcessNAPJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.NAP> NAPRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IBlobStorageService blobStorageService,
        IUserContextService userContextService,
        IBackgroundJobService backgroundJobService,
        IGenerateConnectivityAssessmentJob generateConnectivityAssessmentJob,
        IMemoryCache memoryCache
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _NAPRepository = NAPRepository;
        _blobStorageService = blobStorageService;
        _userContextService = userContextService;
        _backgroundJobService = backgroundJobService;
        _generateConnectivityAssessmentJob = generateConnectivityAssessmentJob;
        _memoryCache = memoryCache;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        var processingResults = new ProcessingResults();

        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(
       parameters.Id, _fileRepository, _logger, FileTypeEnum.NAP, token);

        await ProcessingJobHelper.SetFileProcessingStatusAsync(
     file, _fileRepository, _logger, FileTypeEnum.NAP, token);

        try
        {
            _logger.LogInformation("Starting NAP processing for file: {FileName}", file.Name);

            Stream fileStream;
            try
            {
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(
                     parameters.Path, _blobStorageService, _logger, FileTypeEnum.NAP, token);
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
              ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.NAP, token);
                return;
            }

            stopwatch.Stop();
            _logger.LogInformation("File download completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            using (fileStream)
            {
                stopwatch.Restart();

                _logger.LogInformation("Processing NAP file downloaded from blob storage");
                _logger.LogInformation("NAP processing started");

                var napRecords = ExcelHelper.ParseExcelToClass<NAPRecord>(fileStream);

                if (napRecords.Count is 0)
                {
                    _logger.LogInformation("The Excel is empty.");
                    await ProcessingJobHelper.SetFileSuccessStatusAsync(
                file, _fileRepository, _logger, FileTypeEnum.NAP, token);
                    return;
                }

                _logger.LogInformation("Parsed {RecordCount:N0} records from Excel file", napRecords.Count);
                processingResults.TotalRecords = napRecords.Count;

                // --- Pre-load Reference Data ---
                _logger.LogInformation("Pre-loading reference data.");

                // Preload all service providers
                var providerNames = napRecords.Select(r => r.Provider).Distinct().ToList();
                _logger.LogInformation("Preloading {Count} unique service providers from NAP data...", providerNames.Count);

                var serviceProvidersFromData = await _serviceProviderRepository
                  .QueryAsNoTracking(sp => !sp.IsDeleted && sp.Provider != null)
                     .Include(sp => sp.Provider)
                .Select(sp => new { sp.Id, sp.Provider.Name })
              .ToListAsync(token);

                // Create lookup dictionary for exact matching
                var serviceProviderLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

                foreach (var sp in serviceProvidersFromData)
                {
                    if (sp.Name != null)
                    {
                        serviceProviderLookup[sp.Name] = sp.Id;
                    }
                }

                _logger.LogInformation("Preloaded {Count} service providers", serviceProviderLookup.Count);

                var missingProviders = providerNames.Where(name => !serviceProviderLookup.ContainsKey(name)).ToList();
                if (missingProviders.Any())
                {
                    _logger.LogWarning("Missing service providers from database:");
                    foreach (var missing in missingProviders)
                    {
                        _logger.LogWarning("  - '{Provider}'", missing);
                    }
                }

                var validProviderIds = serviceProviderLookup.Values.ToList();

                // --- Pre-load Existing NAPs using Hash-Based Duplicate Detection (OPTIMIZED) ---
                _logger.LogInformation("Pre-loading existing NAPs for duplicate checking...");

                var napLookup = new HashSet<int>(); // Hash-based lookup (4 bytes per NAP!)
                var totalLoadedNaps = 0;
                var totalNaps = await _NAPRepository
     .Query(nap => !nap.IsDeleted && validProviderIds.Contains(nap.ServiceProviderId))
               .CountAsync(token);

                _logger.LogInformation("Loading {TotalCount} existing NAPs in batches of {BatchSize}...",
                     totalNaps, DUPLICATE_LOAD_BATCH_SIZE);

                // Load existing NAPs in batches to avoid memory overload (following ProcessCellJobBatched pattern)
                var skip = 0;
                while (true)
                {
                    var existingBatch = await _NAPRepository
                        .Query(nap => !nap.IsDeleted && validProviderIds.Contains(nap.ServiceProviderId))
                       .OrderBy(nap => nap.Id) // Required for consistent pagination
                    .Skip(skip)
                    .Take(DUPLICATE_LOAD_BATCH_SIZE)
                           .Select(nap => new
                           {
                               nap.ServiceProviderId,
                               nap.Address,
                               nap.Location
                           })
                  .ToListAsync(token);

                    if (existingBatch.Count == 0)
                        break;

                    foreach (var nap in existingBatch)
                    {
                        var hash = GenerateNAPHash(nap.ServiceProviderId, nap.Address, nap.Location);
                        napLookup.Add(hash);
                    }

                    totalLoadedNaps += existingBatch.Count;
                    skip += DUPLICATE_LOAD_BATCH_SIZE;

                    _logger.LogInformation("Loaded {LoadedCount}/{TotalCount} existing NAPs...",
                      totalLoadedNaps, totalNaps);
                }

                _logger.LogInformation("Pre-loaded {Count} unique NAP records for duplicate checking", napLookup.Count);

                stopwatch.Stop();
                _logger.LogInformation("Pre-loading reference data completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

                // --- Process Records in Batches (OPTIMIZED) ---
                var totalRecords = napRecords.Count;
                var totalBatches = (int)Math.Ceiling((double)totalRecords / BATCH_SIZE);

                _logger.LogInformation("Processing {TotalRecords:N0} records in {TotalBatches} batches of {BatchSize}",
        totalRecords, totalBatches, BATCH_SIZE);

                stopwatch.Restart();

                // Get user context once for all batches
                var userId = _userContextService.GetCurrentUserId() ?? "System";
                var now = DateTime.UtcNow;

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var batch = napRecords.Skip(batchIndex * BATCH_SIZE).Take(BATCH_SIZE).ToList();
                    var batchNumber = batchIndex + 1;
                    var napsToAdd = new List<Domain.Aggregates.NAP>();
                    var duplicatesSkipped = 0;
                    var errorsInBatch = 0;

                    _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({RecordCount} records)",
                         batchNumber, totalBatches, batch.Count);

                    foreach (var record in batch)
                    {
                        processingResults.ProcessedRecords++;

                        try
                        {
                            if (!serviceProviderLookup.TryGetValue(record.Provider, out var serviceProviderId))
                            {
                                _logger.LogWarning("Service Provider not found in preloaded data: {Provider}. Skipping record.", record.Provider);
                                processingResults.SkippedRecords++;
                                errorsInBatch++;
                                continue;
                            }

                            // Validate coordinates
                            if (record.Latitude < -90 || record.Latitude > 90 ||
                      record.Longitude < -180 || record.Longitude > 180)
                            {
                                _logger.LogWarning("Invalid coordinates for record: {Provider}, {Municipality} {Province} ({Latitude},{Longitude})",
                                  record.Provider, record.Municipality, record.Province, record.Latitude, record.Longitude);
                                processingResults.SkippedRecords++;
                                errorsInBatch++;
                                continue;
                            }

                            var address = $"{record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";
                            var location = GeometryHelper.CreatePoint(new Coordinate(y: record.Latitude, x: record.Longitude));

                            // Check for duplicate using hash (OPTIMIZED - 4 bytes vs 80-100 bytes string key)
                            var napHash = GenerateNAPHash(serviceProviderId, address, location);

                            if (napLookup.Contains(napHash))
                            {
                                duplicatesSkipped++;
                                processingResults.SkippedDuplicates++;
                                continue;
                            }

                            // Add to lookup to prevent future duplicates in this session
                            napLookup.Add(napHash);

                            var nap = new Domain.Aggregates.NAP
                            {
                                ServiceProviderId = serviceProviderId,
                                Address = address,
                                Location = location,
                                // CRITICAL: Manually set audit properties since BulkInsertAsync bypasses interceptors
                                CreatedAt = now,
                                CreatedBy = userId,
                                UpdatedAt = now,
                                UpdatedBy = userId
                            };

                            napsToAdd.Add(nap);
                            processingResults.SuccessfulRecords++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error processing record: {Provider}, {Municipality} {Province}. Error: {Message}",
                            record.Provider, record.Municipality, record.Province, ex.Message);
                            processingResults.ErrorRecords++;
                            errorsInBatch++;
                            continue;
                        }
                    }

                    // Save batch to database
                    if (napsToAdd.Count > 0)
                    {
                        try
                        {
                            await _NAPRepository.BulkInsertAsync(napsToAdd, token);
                            processingResults.NAPsCreated += napsToAdd.Count;
                            _logger.LogInformation("Batch {BatchNumber}: Successfully saved {Count} NAP records to database",
                              batchNumber, napsToAdd.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error saving batch {BatchNumber} to database: {Message}",
                              batchNumber, ex.Message);
                            processingResults.ErrorRecords += napsToAdd.Count;
                            // Continue to next batch (per-batch error recovery)
                        }
                    }

                    // Detailed progress reporting (following ProcessCellJobBatched pattern)
                    var processedCount = Math.Min((batchIndex + 1) * BATCH_SIZE, totalRecords);
                    var progress = (double)processedCount / totalRecords * 100;
                    ProcessingJobHelper.ReportProgress(
                        $"Batch {batchNumber}/{totalBatches} completed - Progress: {progress:F1}% - " +
                 $"Successful: {processingResults.SuccessfulRecords:N0}, Errors: {processingResults.ErrorRecords:N0}, " +
                     $"NAPs Created: {processingResults.NAPsCreated:N0}, " +
                   $"Duplicates Skipped: {processingResults.SkippedDuplicates:N0}",
                    _logger);
                }

                stopwatch.Stop();
                _logger.LogInformation("Record processing completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

                // --- Complete Processing with Comprehensive Summary ---
                await CompleteProcessing(file, processingResults, startTime, token);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error occurred during NAP processing after {ElapsedTime}: {ErrorMessage}",
      stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"), ex.Message);
            await ProcessingJobHelper.TerminateProcessAsync(
                file, _fileRepository, _logger, FileTypeEnum.NAP,
               $"Error occurred during processing: {ex.Message}", token);
            throw;
        }
    }

    /// <summary>
    /// Generates a hash code for NAP duplicate detection (following ProcessCellJobBatched pattern)
    /// Hash size: 4 bytes vs 80-100 bytes for string keys = 95% memory reduction!
    /// </summary>
    private static int GenerateNAPHash(Guid serviceProviderId, string address, Point location)
    {
        var hash = new HashCode();
        hash.Add(serviceProviderId);
        hash.Add(address ?? string.Empty);
        // Round coordinates to 6 decimal places (accurate to ~11cm)
        hash.Add(Math.Round(location.Coordinate.X, 6));
        hash.Add(Math.Round(location.Coordinate.Y, 6));
        return hash.ToHashCode();
    }

    /// <summary>
    /// Completes processing and logs comprehensive summary (following ProcessCellJobBatched pattern)
    /// </summary>
    private async Task CompleteProcessing(
        Domain.Entities.File file,
        ProcessingResults results,
        DateTime startTime,
        CancellationToken token)
    {
        var processingTime = DateTime.UtcNow - startTime;
        var successRate = results.TotalRecords > 0
            ? (double)results.SuccessfulRecords / results.TotalRecords * 100
            : 0;

        _logger.LogInformation("=== NAP Processing Summary ===");
        _logger.LogInformation("File: {FileName}", file.Name);
        _logger.LogInformation("Processing Time: {ProcessingTime:hh\\:mm\\:ss}", processingTime);
        _logger.LogInformation("Total Records: {TotalRecords:N0}", results.TotalRecords);
        _logger.LogInformation("Processed Records: {ProcessedRecords:N0}", results.ProcessedRecords);
        _logger.LogInformation("Successful Records: {SuccessfulRecords:N0} ({SuccessRate:F1}%)",
            results.SuccessfulRecords, successRate);
        _logger.LogInformation("Skipped Records: {SkippedRecords:N0}", results.SkippedRecords);
        _logger.LogInformation("Error Records: {ErrorRecords:N0}", results.ErrorRecords);

        _logger.LogInformation("Entities Created:");
        _logger.LogInformation("  NAPs: {NAPsCreated:N0}", results.NAPsCreated);

        _logger.LogInformation("Duplicates Skipped: {SkippedDuplicates:N0}", results.SkippedDuplicates);

        if (results.TotalRecords > 0)
        {
            var processingRate = results.TotalRecords / processingTime.TotalSeconds;
            _logger.LogInformation("Processing Rate: {ProcessingRate:F0} records/second", processingRate);

            var totalPotentialNAPs = results.NAPsCreated + results.SkippedDuplicates;
            var duplicateRate = totalPotentialNAPs > 0
                ? (double)results.SkippedDuplicates / totalPotentialNAPs * 100
                : 0;
            _logger.LogInformation("Duplicate Rate: {DuplicateRate:F1}% ({SkippedDuplicates:N0} of {TotalPotential:N0} potential NAPs)",
                duplicateRate, results.SkippedDuplicates, totalPotentialNAPs);
        }

        _logger.LogInformation("Processing Logic Applied:");
        _logger.LogInformation("- Hash-based duplicate detection (4-byte hashes) - 95% memory reduction");
        _logger.LogInformation("- Batched duplicate loading (10K chunks) - prevents OOM on large databases");
        _logger.LogInformation("- Batch processing (5000 records per batch) - optimized for simple entity");
        _logger.LogInformation("- Per-batch saves for excellent error recovery");
        _logger.LogInformation("- Coordinate validation (lat: -90 to 90, lon: -180 to 180)");
        _logger.LogInformation("- Service provider pre-loading and caching");
        _logger.LogInformation("- AsNoTracking queries for read operations");
        _logger.LogInformation("- Field projection to minimize network transfer");
        _logger.LogInformation("=======================================");

        file.Status = results.ErrorRecords == 0 ? FileStatusEnum.Success : FileStatusEnum.PartiallyProcessed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        // Invalidate the NAPs memory cache so subsequent list/viewport requests reflect the newly imported data
        ProcessingJobHelper.InvalidateCacheKeySet(_memoryCache, NAPsCacheKeysSet, _logger);

        // Trigger connectivity assessment generation upon successful processing
        if (file.Status == FileStatusEnum.Success || file.Status == FileStatusEnum.PartiallyProcessed)
        {
            _logger.LogInformation("NAP processing completed successfully. Triggering connectivity assessment generation for file {FileId}", file.Id);
            await _generateConnectivityAssessmentJob.ExecuteAsync(file.Id, token);
        }

        _logger.LogInformation("NAP processing completed successfully.");
    }

    /// <summary>
    /// Container for processing results and statistics (following ProcessCellJobBatched pattern)
    /// </summary>
    private class ProcessingResults
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int ErrorRecords { get; set; }
        public int NAPsCreated { get; set; }
        public int SkippedDuplicates { get; set; }
    }
}

