using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Attributes;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;
using System.Diagnostics;
using System.Reflection;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessWirelessValidationJob(IFileRepository fileRepository,
                                          ILogger<ProcessWirelessValidationJob> logger,
                                          IMunicipalityRepository municipalityRepository,
                                          IBarangayRepository barangayRepository,
                                          IServiceProviderRepository serviceProviderRepository,
                                          ITechnologyRepository technologyRepository,
                                          IBandRepository bandRepository,
                                          IWirelessValidationRepository wirelessValidationRepository,
                                          IBlobStorageService blobStorageService,
                                          IUserContextService userContextService,
                                          IBackgroundJobService backgroundJobService,
                                          IGenerateConnectivityAssessmentJob generateConnectivityAssessmentJob
                                          ) : IProcessWirelessValidationJob
{
    private readonly IBackgroundJobService _backgroundJobService = backgroundJobService;
    private const double MinimumMunicipalityMatchThreshold = 0.8;
    private const double MinimumProvinceMatchThreshold = 0.6;
    private const double MinimumRegionMatchThreshold = 0.5;

    // OPTIMIZED: Batch processing configuration (like ProcessCellJob)
    private const int BATCH_SIZE = 500; // Process 500 records per batch
    private const int DUPLICATE_LOAD_BATCH_SIZE = 10000; // Load existing records in batches for memory efficiency

    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly ILogger<ProcessWirelessValidationJob> _logger = logger;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly IWirelessValidationRepository _wirelessValidationRepository = wirelessValidationRepository;
    private readonly IBlobStorageService _blobStorageService = blobStorageService;
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IGenerateConnectivityAssessmentJob _generateConnectivityAssessmentJob = generateConnectivityAssessmentJob;

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        var processingResults = new ProcessingResults();

        // --- Step 1: Initial File Status Update ---
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(parameters.Id, _fileRepository, _logger, FileTypeEnum.WirelessValidation, token);
        await ProcessingJobHelper.SetFileProcessingStatusAsync(file, _fileRepository, _logger, FileTypeEnum.WirelessValidation, token);

        try
        {
            _logger.LogInformation("Starting wireless validation processing for file: {FileName}", file.Name);

            // --- Step 2: Download file from blob storage ---
            Stream fileStream;
            try
            {
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(parameters.Path, _blobStorageService, _logger, FileTypeEnum.WirelessValidation, token);
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.WirelessValidation, token);
                return;
            }

            stopwatch.Stop();
            _logger.LogInformation("File download completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            List<WirelessValidationRecord> wirelessValidationRecords;
            using (fileStream)
            {
                stopwatch.Restart();
                _logger.LogInformation("Processing Wireless Validation file from blob storage");
                wirelessValidationRecords = WirelessValidationExcelHelper.ParseExcelToWirelessValidationRecords(fileStream);
                stopwatch.Stop();
                _logger.LogInformation("Excel parsing completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            }

            if (wirelessValidationRecords.Count == 0)
            {
                _logger.LogInformation("The Excel file is empty or contains no valid data.");
                await CompleteProcessing(file, processingResults, null, startTime, token);
                return;
            }

            _logger.LogInformation("Parsed {RecordCount:N0} records from Excel file", wirelessValidationRecords.Count);
            processingResults.TotalRecords = wirelessValidationRecords.Count;

            // --- Step 3: Pre-load Static/Reference Data for Performance ---
            stopwatch.Restart();
            var referenceData = await PreloadReferenceData(wirelessValidationRecords, token);
            stopwatch.Stop();
            _logger.LogInformation("Reference data preloaded successfully in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            // --- Step 4: Resolve Municipality using Hierarchical Approach ---
            stopwatch.Restart();
            await ResolveMunicipalityHierarchical(wirelessValidationRecords, referenceData, token);
            stopwatch.Stop();
            _logger.LogInformation("Municipality resolution completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            // --- Step 5: Process Records in Batches (OPTIMIZED) ---
            stopwatch.Restart();
            var errorLogger = new ImportErrorLogger();
            await ProcessRecordsInBatches(wirelessValidationRecords, referenceData, processingResults, errorLogger, token);
            stopwatch.Stop();
            _logger.LogInformation("Record processing completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            await CompleteProcessing(file, processingResults, errorLogger, startTime, token);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error occurred during wireless validation processing after {ElapsedTime}: {ErrorMessage}",
                stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"), ex.Message);
            await ProcessingJobHelper.TerminateProcessAsync(file, _fileRepository, _logger, FileTypeEnum.WirelessValidation,
                $"Error occurred during processing: {ex.Message}", token);
            throw;
        }
    }

    // OPTIMIZED: Like ProcessCellJob - preload with hash-based duplicate detection
    private async Task<ReferenceData> PreloadReferenceData(List<WirelessValidationRecord> records, CancellationToken token)
    {
        _logger.LogInformation("Pre-loading reference data.");

        // Pre-load wireless service providers
        var serviceProviderCache = await _serviceProviderRepository
            .Query(sp => sp.Service.Type == ServiceTypeEnum.Wireless)
            .Select(sp => new { sp.Id, sp.Provider.Name })
            .ToDictionaryAsync(
                sp => CacheKeyGenerator.GenerateServiceProviderCacheKey(sp.Name, ServiceTypeEnum.Wireless),
                sp => sp.Id,
                token
            );
        _logger.LogInformation("Pre-loaded {Count:N0} wireless service providers.", serviceProviderCache.Count);

        // Pre-load hierarchical municipality data
        var municipalityHierarchyList = await _municipalityRepository
            .QueryAsNoTracking()
            .Select(m => new
            {
                m.Id,
                m.Name,
                ProvinceName = m.Province.Name,
                RegionName = m.Province.Region.Name,
                m.ProvinceId,
                RegionId = m.Province.RegionId
            })
            .ToListAsync(token);

        var municipalityHierarchyCache = new Dictionary<string, MunicipalityHierarchyInfo>();
        foreach (var municipality in municipalityHierarchyList)
        {
            var key = GenerateHierarchicalMunicipalityKey(municipality.Name, municipality.ProvinceName, municipality.RegionName);
            municipalityHierarchyCache[key] = new MunicipalityHierarchyInfo
            {
                Id = municipality.Id,
                Name = municipality.Name,
                ProvinceName = municipality.ProvinceName,
                RegionName = municipality.RegionName,
                ProvinceId = municipality.ProvinceId,
                RegionId = municipality.RegionId
            };
        }

        var municipalityCache = new Dictionary<string, Guid>();
        foreach (var municipality in municipalityHierarchyList)
        {
            var key = CacheKeyGenerator.GenerateMunicipalityCacheKey(municipality.Name);
            if (!municipalityCache.ContainsKey(key))
            {
                municipalityCache[key] = municipality.Id;
            }
        }
        _logger.LogInformation("Pre-loaded {Count:N0} municipalities with hierarchical data", municipalityHierarchyCache.Count);

        // Pre-load technologies
        var technologyList = await _technologyRepository
            .QueryAsNoTracking()
            .Select(t => new { t.Id, Technology = t.Standard })
            .ToListAsync(token);

        var technologyCache = new Dictionary<string, Guid>();
        foreach (var tech in technologyList)
        {
            var key = CacheKeyGenerator.GenerateTechnologyCacheKey(tech.Technology);
            technologyCache[key] = tech.Id;
        }
        _logger.LogInformation("Pre-loaded {Count:N0} technologies.", technologyCache.Count);
        TechnologyMappingHelper.LogTechnologyDiagnostics(technologyCache, _logger);

        // Pre-load bands
        var bandList = await _bandRepository
            .QueryAsNoTracking()
            .Select(b => new { b.Id, Band = b.Name })
            .ToListAsync(token);

        var bandCache = new Dictionary<string, Guid>();
        foreach (var band in bandList)
        {
            var key = CacheKeyGenerator.GenerateBandCacheKey(band.Band);
            bandCache[key] = band.Id;
        }
        _logger.LogInformation("Pre-loaded {Count:N0} bands.", bandCache.Count);

        // OPTIMIZED: Hash-based duplicate detection (like ProcessCellJob)
        var serviceProviderIds = serviceProviderCache.Values.ToList();
        var validationLookup = new HashSet<int>();
        var totalLoadedValidations = 0;
        var totalValidations = await _wirelessValidationRepository
            .Query(wv => serviceProviderIds.Contains(wv.ServiceProviderId))
            .CountAsync(token);

        _logger.LogInformation("Loading {TotalCount} existing wireless validation records in batches of {BatchSize}...",
            totalValidations, DUPLICATE_LOAD_BATCH_SIZE);

        // Load existing validations in batches to avoid memory overload
        var skip = 0;
        while (true)
        {
            var existingBatch = await _wirelessValidationRepository
                .Query(wv => serviceProviderIds.Contains(wv.ServiceProviderId))
                .OrderBy(wv => wv.Id)
                .Skip(skip)
                .Take(DUPLICATE_LOAD_BATCH_SIZE)
                .Select(wv => new
                {
                    wv.ServiceProviderId,
                    wv.BarangayId,
                    wv.BandId,
                    wv.TechnologyId,
                    wv.Address
                })
                .ToListAsync(token);

            if (existingBatch.Count == 0)
                break;

            foreach (var validation in existingBatch)
            {
                var hash = GenerateWirelessValidationHash(
                    validation.ServiceProviderId,
                    validation.BarangayId,
                    validation.BandId,
                    validation.TechnologyId,
                    validation.Address ?? string.Empty
                );

                validationLookup.Add(hash);
            }

            totalLoadedValidations += existingBatch.Count;
            skip += DUPLICATE_LOAD_BATCH_SIZE;

            _logger.LogInformation("Loaded {LoadedCount}/{TotalCount} existing wireless validation records...",
                totalLoadedValidations, totalValidations);
        }

        _logger.LogInformation("Pre-loaded {Count:N0} unique wireless validation records.", validationLookup.Count);

        return new ReferenceData
        {
            ServiceProviderCache = serviceProviderCache,
            MunicipalityCache = municipalityCache,
            MunicipalityHierarchyCache = municipalityHierarchyCache,
            TechnologyCache = technologyCache,
            BandCache = bandCache,
            WirelessValidationLookup = validationLookup
        };
    }

    /// <summary>
    /// Generates a hash code for wireless validation duplicate detection
    /// </summary>
    private static int GenerateWirelessValidationHash(Guid serviceProviderId, Guid barangayId, Guid bandId, Guid technologyId, string address)
    {
        var hash = new HashCode();
        hash.Add(serviceProviderId);
        hash.Add(barangayId);
        hash.Add(bandId);
        hash.Add(technologyId);
        hash.Add(address?.ToLowerInvariant().Trim() ?? string.Empty);
        return hash.ToHashCode();
    }

    private async Task ResolveMunicipalityHierarchical(List<WirelessValidationRecord> records, ReferenceData referenceData, CancellationToken token)
    {
        var municipalityGroups = records.GroupBy(r => new { r.Municipality, r.Province, r.Region }).ToList();
        _logger.LogInformation("Resolving {Count} unique municipality combinations", municipalityGroups.Count);

        foreach (var group in municipalityGroups)
        {
            var municipalityName = group.Key.Municipality;
            var provinceName = group.Key.Province;
            var regionName = group.Key.Region;

            MunicipalityHierarchyInfo? resolvedMunicipality = null;

            var hierarchicalKey = GenerateHierarchicalMunicipalityKey(municipalityName, provinceName, regionName);
            if (referenceData.MunicipalityHierarchyCache.TryGetValue(hierarchicalKey, out resolvedMunicipality))
            {
                _logger.LogDebug("Exact hierarchical match found for: {Municipality}, {Province}, {Region}",
                    municipalityName, provinceName, regionName);
            }
            else
            {
                resolvedMunicipality = await FindMunicipalityWithFuzzyMatching(municipalityName, provinceName, regionName, referenceData, token);
            }

            if (resolvedMunicipality != null)
            {
                // Load barangays for this municipality if not already loaded
                if (!referenceData.BarangayCache.ContainsKey(resolvedMunicipality.Id))
                {
                    var barangayList = await _barangayRepository
                        .QueryAsNoTracking(b => b.MunicipalityId == resolvedMunicipality.Id)
                        .Select(b => new { b.Id, b.Name, b.MunicipalityId })
                        .ToListAsync(token);

                    var barangayCache = new Dictionary<string, Guid>();
                    foreach (var barangay in barangayList)
                    {
                        var key = CacheKeyGenerator.GenerateBarangayCacheKey(barangay.Name, barangay.MunicipalityId);
                        barangayCache[key] = barangay.Id;
                    }

                    referenceData.BarangayCache[resolvedMunicipality.Id] = barangayCache;
                    _logger.LogInformation("Pre-loaded {Count:N0} barangays for municipality: {Municipality}",
                        barangayCache.Count, resolvedMunicipality.Name);
                }

                foreach (var record in group)
                {
                    record.MunicipalityId = resolvedMunicipality.Id;
                }

                _logger.LogInformation("Resolved {RecordCount} records for municipality: {Municipality} (ID: {MunicipalityId})",
                    group.Count(), resolvedMunicipality.Name, resolvedMunicipality.Id);
            }
            else
            {
                _logger.LogWarning("Municipality not found: {Municipality}, {Province}, {Region}. {RecordCount} records will be skipped.",
                    municipalityName, provinceName, regionName, group.Count());
            }
        }
    }

    private Task<MunicipalityHierarchyInfo?> FindMunicipalityWithFuzzyMatching(string municipalityName, string provinceName, string regionName, ReferenceData referenceData, CancellationToken token)
    {
        _logger.LogInformation("Attempting fuzzy match for: {Municipality}, {Province}, {Region}", municipalityName, provinceName, regionName);

        var possibleMatches = new List<MunicipalityHierarchyInfo>();

        foreach (var kvp in referenceData.MunicipalityHierarchyCache)
        {
            var municipalityInfo = kvp.Value;

            var municipalityScore = CalculateSimilarity(municipalityName, municipalityInfo.Name);
            var provinceScore = CalculateSimilarity(provinceName, municipalityInfo.ProvinceName);
            var regionScore = CalculateSimilarity(regionName, municipalityInfo.RegionName);

            if (municipalityScore >= MinimumMunicipalityMatchThreshold &&
                provinceScore >= MinimumProvinceMatchThreshold &&
                regionScore >= MinimumRegionMatchThreshold)
            {
                possibleMatches.Add(municipalityInfo);
                _logger.LogDebug("Fuzzy match candidate: {Municipality} ({MScore:F2}), {Province} ({PScore:F2}), {Region} ({RScore:F2})",
                    municipalityInfo.Name, municipalityScore, municipalityInfo.ProvinceName, provinceScore, municipalityInfo.RegionName, regionScore);
            }
        }

        if (possibleMatches.Count == 1)
        {
            _logger.LogInformation("Single fuzzy match found: {Municipality}, {Province}, {Region}",
                possibleMatches[0].Name, possibleMatches[0].ProvinceName, possibleMatches[0].RegionName);
            return Task.FromResult<MunicipalityHierarchyInfo?>(possibleMatches[0]);
        }
        else if (possibleMatches.Count > 1)
        {
            var bestMatch = possibleMatches
                .Select(m => new
                {
                    Municipality = m,
                    Score = (CalculateSimilarity(municipalityName, m.Name) * 2) +
                           CalculateSimilarity(provinceName, m.ProvinceName) +
                           CalculateSimilarity(regionName, m.RegionName)
                })
                .OrderByDescending(x => x.Score)
                .First();

            _logger.LogInformation("Best fuzzy match selected: {Municipality}, {Province}, {Region} (Score: {Score:F2})",
                bestMatch.Municipality.Name, bestMatch.Municipality.ProvinceName, bestMatch.Municipality.RegionName, bestMatch.Score);
            return Task.FromResult<MunicipalityHierarchyInfo?>(bestMatch.Municipality);
        }

        _logger.LogWarning("No suitable fuzzy match found for: {Municipality}, {Province}, {Region}", municipalityName, provinceName, regionName);
        return Task.FromResult<MunicipalityHierarchyInfo?>(null);
    }

    private static double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0;

        source = source.ToLowerInvariant().Trim();
        target = target.ToLowerInvariant().Trim();

        if (source == target)
            return 1.0;

        if (source.Contains(target) || target.Contains(source))
            return 0.9;

        var distance = LevenshteinDistance(source, target);
        var maxLength = Math.Max(source.Length, target.Length);
        return 1.0 - (double)distance / maxLength;
    }

    private static int LevenshteinDistance(string source, string target)
    {
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= target.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    private static string GenerateHierarchicalMunicipalityKey(string municipality, string province, string region)
    {
        return $"{municipality.ToLowerInvariant().Trim()}|{province.ToLowerInvariant().Trim()}|{region.ToLowerInvariant().Trim()}";
    }

    // OPTIMIZED: Batch processing like ProcessCellJob (periodic saves instead of single bulk insert)
    private async Task ProcessRecordsInBatches(List<WirelessValidationRecord> records, ReferenceData referenceData,
        ProcessingResults results, ImportErrorLogger errorLogger, CancellationToken token)
    {
        // Get user context once for all batches
        var userId = _userContextService.GetCurrentUserId() ?? "System";
        var now = DateTime.UtcNow;

        var totalRecords = records.Count;
        var totalBatches = (int)Math.Ceiling((double)totalRecords / BATCH_SIZE);

        // Get band/technology properties once
        var bandTechnologyProperties = typeof(WirelessValidationRecord).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(property => new BandTechnologyProperty
            {
                Property = property,
                HeaderAttribute = property.GetCustomAttribute<ExcelHeaderAttribute>(),
                Band = CellPropertyNameParser.GetBandFromPropertyName(property.Name),
                Technology = CellPropertyNameParser.GetTechnologyFromPropertyName(property.Name)
            })
            .Where(item => item.HeaderAttribute != null && !string.IsNullOrEmpty(item.Band) && !string.IsNullOrEmpty(item.Technology))
            .ToList();

        _logger.LogInformation("Processing {TotalRecords:N0} records in {TotalBatches} batches of {BatchSize}",
      totalRecords, totalBatches, BATCH_SIZE);

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = records.Skip(batchIndex * BATCH_SIZE).Take(BATCH_SIZE).ToList();
            var batchNumber = batchIndex + 1;

            _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({RecordCount} records)",
           batchNumber, totalBatches, batch.Count);

            var validationsToAdd = new List<WirelessValidation>();

            for (int i = 0; i < batch.Count; i++)
            {
                var record = batch[i];
                var actualRowNumber = batchIndex * BATCH_SIZE + i + 5; // Rows start at 5 in Wireless Validation template

                try
                {
                    results.ProcessedRecords++;

                    if (!record.MunicipalityId.HasValue || string.IsNullOrWhiteSpace(record.Provider))
                    {
                        var prov = string.IsNullOrWhiteSpace(record.Provider) ? "<blank>" : record.Provider;
                        errorLogger.AddError(
                            actualRowNumber,
                            "Provider / Municipality",
                            prov,
                            "Provider is missing or Municipality could not be matched.",
                            "Select a valid Provider from dropdown and verify Municipality matches NTC reference data.");
                        results.SkippedRecords++;
                        continue;
                    }

                    var serviceProviderKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wireless);
                    if (!referenceData.ServiceProviderCache.TryGetValue(serviceProviderKey, out var serviceProviderId))
                    {
                        _logger.LogWarning("Service Provider not found: {Provider}. Skipping record at Row {Row}.", record.Provider, actualRowNumber);
                        errorLogger.AddError(
                            actualRowNumber,
                            "Provider",
                            record.Provider,
                            $"Wireless Service Provider '{record.Provider}' was not found in the database.",
                            "Select a valid provider from the template dropdown (e.g., SMART, GLOBE, DITO).");
                        results.SkippedRecords++;
                        continue;
                    }

                    record.ServiceProviderId = serviceProviderId;

                    // Resolve barangay
                    Guid? barangayId = null;
                    if (!string.IsNullOrWhiteSpace(record.Barangay) && record.MunicipalityId.HasValue)
                    {
                        if (referenceData.BarangayCache.TryGetValue(record.MunicipalityId.Value, out var municipalityBarangays))
                        {
                            var barangayKey = CacheKeyGenerator.GenerateBarangayCacheKey(record.Barangay, record.MunicipalityId.Value);
                            if (municipalityBarangays.TryGetValue(barangayKey, out var foundBarangayId))
                            {
                                barangayId = foundBarangayId;
                                record.BarangayId = barangayId;
                            }
                        }
                    }

                    if (!barangayId.HasValue)
                    {
                        _logger.LogWarning("Skipping record without resolved barangay at Row {Row}: Provider={Provider}, Barangay='{Barangay}', Municipality={Municipality}",
                            actualRowNumber, record.Provider, record.Barangay, record.Municipality);
                        var brgy = string.IsNullOrWhiteSpace(record.Barangay) ? "<blank>" : record.Barangay;
                        errorLogger.AddError(
                            actualRowNumber,
                            "Barangay",
                            brgy,
                            $"Barangay '{brgy}' could not be resolved for municipality '{record.Municipality}'.",
                            "Select a valid Barangay from the template dropdown.");
                        results.SkippedRecords++;
                        continue;
                    }

                    // Process each technology/band combination
                    var (recordValidations, skippedDuplicates) = ProcessTechnologyBandCombinations(record, bandTechnologyProperties, referenceData);

                    // CRITICAL: Set audit properties before adding to list
                    foreach (var validation in recordValidations)
                    {
                        validation.CreatedAt = now;
                        validation.CreatedBy = userId;
                        validation.UpdatedAt = now;
                        validation.UpdatedBy = userId;
                    }

                    validationsToAdd.AddRange(recordValidations);
                    results.SkippedDuplicates += skippedDuplicates;

                    if (recordValidations.Count != 0)
                    {
                        results.SuccessfulRecords++;
                        results.CreatedValidations += recordValidations.Count;
                    }
                    else
                    {
                        results.SkippedRecords++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing record at Row {Row} - Provider: {Provider}, Barangay: {Barangay}, Municipality: {Municipality}",
                        actualRowNumber, record.Provider, record.Barangay, record.Municipality);
                    errorLogger.AddError(
                        actualRowNumber,
                        "Record",
                        record.Provider,
                        $"Unexpected error: {ex.Message}",
                        "Verify that all columns follow the Wireless Validation template formatting.");
                    results.ErrorRecords++;
                }
            }

            // OPTIMIZED: Save batch periodically (like ProcessCellJob)
            if (validationsToAdd.Count > 0)
            {
                await _wirelessValidationRepository.BulkInsertAsync(validationsToAdd, token);
                _logger.LogInformation("Batch {BatchNumber}/{TotalBatches}: Added {Count:N0} validations",
                 batchNumber, totalBatches, validationsToAdd.Count);
            }

            // Detailed progress reporting (like ProcessCellJob)
            var processedCount = Math.Min((batchIndex + 1) * BATCH_SIZE, totalRecords);
            var progress = (double)processedCount / totalRecords * 100;
            ProcessingJobHelper.ReportProgress(
                $"Batch {batchNumber}/{totalBatches} completed - Progress: {progress:F1}% - " +
             $"Successful: {results.SuccessfulRecords:N0}, Errors: {results.ErrorRecords:N0}, " +
                $"Validations Created: {results.CreatedValidations:N0}, Duplicates Skipped: {results.SkippedDuplicates:N0}",
      _logger);
        }
    }

    // OPTIMIZED: Hash-based duplicate checking (like ProcessCellJob)
    private (List<WirelessValidation> validations, int skippedDuplicates) ProcessTechnologyBandCombinations(WirelessValidationRecord record,
        List<BandTechnologyProperty> bandTechnologyProperties, ReferenceData referenceData)
    {
        var validations = new List<WirelessValidation>();
        var skippedDuplicates = 0;

        foreach (var item in bandTechnologyProperties)
        {
            try
            {
                var propertyValue = item.Property.GetValue(record) as string;
                var (shouldCreate, rsrpValue, isMarginal) = RSRPParser.ParseWirelessData(propertyValue ?? string.Empty, item.Technology);

                if (!shouldCreate)
                    continue;

                var contextInfo = $"Provider: {record.Provider}, Municipality: {record.Municipality}, Technology: {item.Technology}, Band: {item.Band}";
                var technologyId = TechnologyMappingHelper.ResolveTechnologyId(item.Technology, referenceData.TechnologyCache, _logger, contextInfo);

                if (!TechnologyMappingHelper.IsTechnologyResolved(technologyId))
                {
                    continue;
                }

                var bandId = TechnologyMappingHelper.ResolveBandId(item.Band, referenceData.BandCache, _logger, contextInfo);

                if (!TechnologyMappingHelper.IsBandResolved(bandId))
                {
                    continue;
                }

                // OPTIMIZED: Hash-based duplicate check (like ProcessCellJob)
                var validationHash = GenerateWirelessValidationHash(
                    record.ServiceProviderId!.Value,
                    record.BarangayId!.Value,
                    bandId,
                    technologyId,
                    record.Address ?? string.Empty
                );

                if (referenceData.WirelessValidationLookup.Contains(validationHash))
                {
                    skippedDuplicates++;
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Skipping duplicate wireless validation - Provider: {Provider}, Technology: {Technology}, Band: {Band}",
                            record.Provider, item.Technology, item.Band);
                    }
                    continue;
                }

                var validation = new WirelessValidation
                {
                    ServiceProviderId = record.ServiceProviderId!.Value,
                    BarangayId = record.BarangayId!.Value,
                    BandId = bandId,
                    TechnologyId = technologyId,
                    Address = record.Address,
                    RSRP = rsrpValue,
                    IsMarginal = isMarginal
                };

                validations.Add(validation);
                referenceData.WirelessValidationLookup.Add(validationHash);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var qualityDesc = rsrpValue.HasValue
                        ? RSRPParser.GetSignalQualityDescription(rsrpValue.Value, item.Technology)
                        : (isMarginal ? "Marginal (No RSRP)" : "Good (No RSRP)");

                    _logger.LogDebug("Created validation - Provider: {Provider}, Technology: {Technology}, Band: {Band}, " +
                        "RSRP: {RSRP}, Marginal: {IsMarginal}, Quality: {Quality}",
                        record.Provider, item.Technology, item.Band,
                        rsrpValue?.ToString() ?? "NULL", isMarginal, qualityDesc);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing technology/band combination: {Technology}/{Band} for record {Provider}",
                    item.Technology, item.Band, record.Provider);
            }
        }

        return (validations, skippedDuplicates);
    }

    private async Task CompleteProcessing(
        Domain.Entities.File file,
        ProcessingResults results,
        ImportErrorLogger? errorLogger,
        DateTime startTime,
        CancellationToken token)
    {
        var processingTime = DateTime.UtcNow - startTime;
        var successRate = results.TotalRecords > 0 ? (double)results.SuccessfulRecords / results.TotalRecords * 100 : 0;

        _logger.LogInformation("=== Wireless Validation Processing Summary ===");
        _logger.LogInformation("File: {FileName}", file.Name);
        _logger.LogInformation("Processing Time: {ProcessingTime:hh\\:mm\\:ss}", processingTime);
        _logger.LogInformation("Total Records: {TotalRecords:N0}", results.TotalRecords);
        _logger.LogInformation("Processed Records: {ProcessedRecords:N0}", results.ProcessedRecords);
        _logger.LogInformation("Successful Records: {SuccessfulRecords:N0} ({SuccessRate:F1}%)",
            results.SuccessfulRecords, successRate);
        _logger.LogInformation("Skipped Records: {SkippedRecords:N0}", results.SkippedRecords);
        _logger.LogInformation("Error Records: {ErrorRecords:N0}", results.ErrorRecords);
        _logger.LogInformation("Wireless Validations Created: {CreatedValidations:N0}", results.CreatedValidations);
        _logger.LogInformation("Duplicate Validations Skipped: {SkippedDuplicates:N0}", results.SkippedDuplicates);

        if (results.TotalRecords > 0)
        {
            var processingRate = results.TotalRecords / processingTime.TotalSeconds;
            _logger.LogInformation("Processing Rate: {ProcessingRate:F0} records/second", processingRate);

            var avgValidationsPerRecord = results.SuccessfulRecords > 0
                ? (double)results.CreatedValidations / results.SuccessfulRecords
                : 0;
            _logger.LogInformation("Average Validations per Record: {AvgValidations:F1}", avgValidationsPerRecord);

            var totalPotentialValidations = results.CreatedValidations + results.SkippedDuplicates;
            var duplicateRate = totalPotentialValidations > 0
                ? (double)results.SkippedDuplicates / totalPotentialValidations * 100
                : 0;
            _logger.LogInformation("Duplicate Rate: {DuplicateRate:F1}% ({SkippedDuplicates:N0} of {TotalPotential:N0} potential validations)",
                duplicateRate, results.SkippedDuplicates, totalPotentialValidations);
        }

        _logger.LogInformation("Processing Logic Applied:");
        _logger.LogInformation("- Hash-based duplicate detection (4-byte hashes)");
        _logger.LogInformation("- Batched duplicate loading (10K chunks)");
        _logger.LogInformation("- Batch processing (500 records per batch)");
        _logger.LogInformation("- Hierarchical municipality resolution with Province and Region validation");
        _logger.LogInformation("- Fuzzy matching for address variations and spelling differences");
        _logger.LogInformation("- Barangay resolution is REQUIRED - records without resolved barangay are skipped");
        _logger.LogInformation("- Consistent address construction from all available location components");
        _logger.LogInformation("- 'false', empty, N/A values: Skipped (no entity created)");
        _logger.LogInformation("- 'marginal' values: Created with IsMarginal=true, RSRP=NULL");
        _logger.LogInformation("- 'true' values: Created with IsMarginal=false, RSRP=NULL");
        _logger.LogInformation("- RSRP values: Parsed and evaluated per technology thresholds");
        _logger.LogInformation("- NBC Thresholds: 2G/3G <= -105dBm, 4G/5G <= -120dBm (not created)");
        _logger.LogInformation("- Marginal Thresholds: 2G/3G <= -90dBm, 4G/5G <= -100dBm");
        _logger.LogInformation("- Duplicate Detection: ServiceProvider + Barangay + Band + Technology + Address combination");
        _logger.LogInformation("=======================================");

        string? errorLogPath = null;
        if (errorLogger != null && errorLogger.HasErrors)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            errorLogPath = await errorLogger.UploadErrorLogAsync(file, _blobStorageService, env, _logger, token);
            file.ErrorMessage = errorLogger.GenerateSummary();
            file.ErrorLogPath = errorLogPath;
        }

        if (results.TotalRecords > 0 && results.SuccessfulRecords == 0)
        {
            file.Status = FileStatusEnum.Failed;
            if (string.IsNullOrEmpty(file.ErrorMessage))
            {
                file.ErrorMessage = "All records in the file failed validation or were skipped. Please verify that the file follows the Wireless Validation template specifications.";
            }
        }
        else
        {
            file.Status = results.ErrorRecords == 0 && results.SkippedRecords == 0 ? FileStatusEnum.Success : FileStatusEnum.PartiallyProcessed;
        }
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        // Trigger connectivity assessment generation upon successful processing
        if (file.Status == FileStatusEnum.Success || file.Status == FileStatusEnum.PartiallyProcessed)
        {
            _logger.LogInformation("Wireless validation processing completed successfully. Triggering connectivity assessment generation for file {FileId}", file.Id);
            await _generateConnectivityAssessmentJob.ExecuteAsync(file.Id, token);
        }

        _logger.LogInformation("Processing completed successfully.");
    }

    // Supporting classes
    private class ReferenceData
    {
        public Dictionary<string, Guid> ServiceProviderCache { get; set; } = [];
        public Dictionary<string, Guid> MunicipalityCache { get; set; } = [];
        public Dictionary<string, MunicipalityHierarchyInfo> MunicipalityHierarchyCache { get; set; } = [];
        public Dictionary<string, Guid> TechnologyCache { get; set; } = [];
        public Dictionary<string, Guid> BandCache { get; set; } = [];
        public Dictionary<Guid, Dictionary<string, Guid>> BarangayCache { get; set; } = [];
        public HashSet<int> WirelessValidationLookup { get; set; } = []; // Changed from HashSet<string> to HashSet<int>
    }

    private class MunicipalityHierarchyInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public Guid ProvinceId { get; set; }
        public Guid RegionId { get; set; }
    }

    private class ProcessingResults
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int ErrorRecords { get; set; }
        public int CreatedValidations { get; set; }
        public int SkippedDuplicates { get; set; }
    }

    private class BandTechnologyProperty
    {
        public PropertyInfo Property { get; set; } = null!;
        public ExcelHeaderAttribute? HeaderAttribute { get; set; }
        public string Band { get; set; } = string.Empty;
        public string Technology { get; set; } = string.Empty;
    }
}