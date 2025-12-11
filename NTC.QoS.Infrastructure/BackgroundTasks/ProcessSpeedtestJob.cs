using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Exceptions;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;
using System.Linq.Expressions;
using NTC.QoS.Domain.Attributes;

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

    /// <summary>
    /// Validates a field and terminates processing with a detailed error message if validation fails.
    /// </summary>
    /// <typeparam name="T">The type containing the property to validate</typeparam>
    /// <typeparam name="TProperty">The type of the property being validated</typeparam>
    /// <param name="isValid">The validation condition to check</param>
    /// <param name="propertyExpression">Expression to access the property (e.g., r => r.Provider)</param>
    /// <param name="errorMessage">The error message to display to the user</param>
    /// <param name="rowNumber">The Excel row number where the error occurred</param>
    /// <param name="file">The file entity being processed</param>
    /// <param name="token">Cancellation token</param>
    /// <param name="actualValue">Optional actual value to include in error message</param>
    /// <returns>True if validation passed, throws/returns if validation failed</returns>
    private async Task<bool> ValidateFieldAsync<T, TProperty>(
        bool isValid,
        Expression<Func<T, TProperty>> propertyExpression,
        string errorMessage,
        int rowNumber,
        Domain.Entities.File file,
        CancellationToken token,
        object? actualValue = null)
    {
        if (!isValid)
        {
            // Extract property name and Excel column name from expression
            var memberExpression = (MemberExpression)propertyExpression.Body;
            var propertyInfo = (System.Reflection.PropertyInfo)memberExpression.Member;
            var fieldName = propertyInfo.Name;
            
            // Get Excel column name from ExcelHeader attribute
            var excelHeaderAttr = propertyInfo.GetCustomAttributes(typeof(ExcelHeaderAttribute), false)
                .FirstOrDefault() as ExcelHeaderAttribute;
            var columnName = excelHeaderAttr?.HeaderName ?? fieldName.ToLower();

            var formattedMessage = actualValue != null
                ? $"{errorMessage} at Row {rowNumber}, Column '{columnName}'. Found: {actualValue}"
                : $"{errorMessage} at Row {rowNumber}, Column '{columnName}'";

            _logger.LogWarning("{FieldName} validation failed at Row {Row}. {Message}",
                fieldName, rowNumber, errorMessage);

            await ProcessingJobHelper.TerminateProcessAsync(
                file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                formattedMessage, token);

            return false;
        }
        return true;
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

            // Pre-load all service providers with optimized query
            var serviceProviderCache = await _serviceProviderRepository
                .Query(sp => !sp.IsDeleted)
                .Select(sp => new { sp.Id, sp.Provider.Name, sp.Service.Type })
                .ToDictionaryAsync(
                    sp => CacheKeyGenerator.GenerateServiceProviderCacheKey(sp.Name, sp.Type),
                    sp => sp.Id,
                    token
                );
            _logger.LogInformation("Pre-loaded {Count} service providers.", serviceProviderCache.Count);

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

                List<SpeedtestRecord> speedtestRecords;
                
                try
                {
                    speedtestRecords = ExcelHelper.ParseExcelToClass<SpeedtestRecord>(fileStream);
                }
                catch (ExcelParsingException ex) when (ex.PropertyName?.Equals("Timestamp", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogError(ex, "Failed to parse Excel file. Timestamp field validation error at Row {Row}, Col {Col}.",
                        ex.RowNumber, ex.ColumnNumber);
                    await ProcessingJobHelper.TerminateProcessAsync(
                        file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                        $"Timestamp field is required and cannot be empty at Row {ex.RowNumber}. Please ensure all timestamp values are properly formatted dates.", token);
                    return;
                }
                catch (ExcelParsingException ex)
                {
                    _logger.LogError(ex, "Failed to parse Excel file due to data format error at Row {Row}, Col {Col}, Property {Property}.",
                        ex.RowNumber, ex.ColumnNumber, ex.PropertyName);
                    await ProcessingJobHelper.TerminateProcessAsync(
                        file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                        $"Excel parsing error at Row {ex.RowNumber}, Column '{ex.HeaderName}': {ex.Message}", token);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Excel file due to unexpected error.");
                    await ProcessingJobHelper.TerminateProcessAsync(
                        file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                        $"Unexpected error during Excel parsing: {ex.Message}", token);
                    return;
                }

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

                    var rowNumber = i + 2; // Excel row number (header is row 1, data starts at row 2)
                    foreach (var record in batch)
                    {
                        ProcessingJobHelper.ReportProgress($"Processing record: {record.Region}, {record.Provider}, {record.NetworkType}", _logger);
                        var currentRow = rowNumber++;

                        // TODO: Add unit/integration tests for validation logic to ensure proper error handling
                        // and validation messages are returned when required fields are missing or invalid.
                        // Test cases should cover: empty strings, null values, out-of-range coordinates, zero speeds.

                        // Validate required fields
                        if (!await ValidateFieldAsync(
                            !string.IsNullOrWhiteSpace(record.Provider),
                            (SpeedtestRecord r) => r.Provider,
                            "Provider field is required but was found empty or missing",
                            currentRow,
                            file,
                            token))
                            return;

                        if (!await ValidateFieldAsync(
                            !string.IsNullOrWhiteSpace(record.NetworkType),
                            (SpeedtestRecord r) => r.NetworkType,
                            "NetworkType field is required but was found empty or missing",
                            currentRow,
                            file,
                            token))
                            return;

                        if (!await ValidateFieldAsync(
                            !string.IsNullOrWhiteSpace(record.SignalQuality),
                            (SpeedtestRecord r) => r.SignalQuality,
                            "SignalQuality field is required but was found empty or missing",
                            currentRow,
                            file,
                            token))
                            return;

                        // Validate numeric required fields (Upload, Download, Latitude, Longitude)
                        // Allow 0 speeds (may indicate measurement issues but still valid data to save)
                        // Skip records with negative speeds as they are invalid
                        if (record.Upload < 0)
                        {
                            _logger.LogWarning("Skipping record at Row {Row}: Upload speed cannot be negative. Found: {Value}",
                                currentRow, record.Upload);
                            continue; // Skip this record but continue processing others
                        }

                        if (record.Download < 0)
                        {
                            _logger.LogWarning("Skipping record at Row {Row}: Download speed cannot be negative. Found: {Value}",
                                currentRow, record.Download);
                            continue; // Skip this record but continue processing others
                        }

                        // Skip records where both upload and download are 0 or less (no meaningful speed data)
                        if (record.Upload <= 0 && record.Download <= 0)
                        {
                            _logger.LogWarning("Skipping record at Row {Row}: Both Upload and Download speeds are 0 or less. Upload: {Upload}, Download: {Download}",
                                currentRow, record.Upload, record.Download);
                            continue; // Skip this record but continue processing others
                        }

                        // Validate latitude and longitude (must be valid coordinates)
                        if (!await ValidateFieldAsync(
                            record.Latitude >= -90 && record.Latitude <= 90,
                            (SpeedtestRecord r) => r.Latitude,
                            "Latitude field must be between -90 and 90 degrees",
                            currentRow,
                            file,
                            token,
                            record.Latitude))
                            return;

                        if (!await ValidateFieldAsync(
                            record.Longitude >= -180 && record.Longitude <= 180,
                            (SpeedtestRecord r) => r.Longitude,
                            "Longitude field must be between -180 and 180 degrees",
                            currentRow,
                            file,
                            token,
                            record.Longitude))
                            return;

                        // Check for null island (0,0) using tolerance for floating point comparison
                        const double tolerance = 0.000001; // ~11cm precision
                        if (!await ValidateFieldAsync(
                            !(Math.Abs(record.Latitude) < tolerance && Math.Abs(record.Longitude) < tolerance),
                            (SpeedtestRecord r) => r.Latitude,
                            "Latitude and Longitude fields are required and cannot both be 0",
                            currentRow,
                            file,
                            token))
                            return;

                        // Lookup service provider - try Wireless first, then Wired if not found
                        // This handles both wireless and wired providers since cache includes all types
                        var providerKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wireless);
                        if (!serviceProviderCache.TryGetValue(providerKey, out var serviceProviderId))
                        {
                            // Try Wired type as fallback
                            providerKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wired);
                            if (!serviceProviderCache.TryGetValue(providerKey, out serviceProviderId))
                            {
                                _logger.LogWarning("Service Provider not found: {Provider}. Terminating process.", record.Provider);
                                await ProcessingJobHelper.TerminateProcessAsync(
                                    file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                                    $"Service Provider '{record.Provider}' not found in database", token);
                                return;
                            }
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
                            
                            // Parse SignalQuality with error handling
                            string signalQuality;
                            try
                            {
                                signalQuality = EnumHelper<SignalQualityEnum>.ParseUserInput(record.SignalQuality).ToString();
                            }
                            catch (ArgumentException ex)
                            {
                                _logger.LogWarning("Invalid SignalQuality value: {SignalQuality}. Terminating process. Error: {Error}", 
                                    record.SignalQuality, ex.Message);
                                await ProcessingJobHelper.TerminateProcessAsync(
                                    file, _fileRepository, _logger, FileTypeEnum.Speedtest,
                                    $"Invalid SignalQuality value '{record.SignalQuality}'. {ex.Message}", token);
                                return;
                            }

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