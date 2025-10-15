using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Attributes;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;
using System.Reflection;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessWirelessValidationJob(IFileRepository fileRepository,
                                          ILogger<ProcessWirelessValidationJob> logger,
                                          IRegionRepository regionRepository,
                                          IProvinceRepository provinceRepository,
                                          IMunicipalityRepository municipalityRepository,
                                          IBarangayRepository barangayRepository,
                                          IServiceProviderRepository serviceProviderRepository,
                                          ITechnologyRepository technologyRepository,
                                          IBandRepository bandRepository,
                                          IWirelessValidationRepository wirelessValidationRepository,
                                          IBlobStorageService blobStorageService
                                          ) : IProcessWirelessValidationJob
{
    private const double MinimumMunicipalityMatchThreshold = 0.8;
    private const double MinimumProvinceMatchThreshold = 0.6;
    private const double MinimumRegionMatchThreshold = 0.5;

    // OPTIMIZED: Single batch size for processing
    private const int ProcessingBatchSize = 1000;

    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly ILogger<ProcessWirelessValidationJob> _logger = logger;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly IWirelessValidationRepository _wirelessValidationRepository = wirelessValidationRepository;
    private readonly IBlobStorageService _blobStorageService = blobStorageService;

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(parameters.Id, _fileRepository, _logger, FileTypeEnum.WirelessValidation, token);
        await ProcessingJobHelper.SetFileProcessingStatusAsync(file, _fileRepository, _logger, FileTypeEnum.WirelessValidation, token);

        var processingResults = new ProcessingResults();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting wireless validation processing for file: {FileName}", file.Name);

            // Download and Parse Excel File
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

            List<WirelessValidationRecord> wirelessValidationRecords;
            using (fileStream)
            {
                _logger.LogInformation("Processing Wireless Validation file from blob storage");
                wirelessValidationRecords = WirelessValidationExcelHelper.ParseExcelToWirelessValidationRecords(fileStream);
            }

            if (wirelessValidationRecords.Count == 0)
            {
                _logger.LogInformation("The Excel file is empty or contains no valid data.");
                await CompleteProcessing(file, processingResults, startTime, token);
                return;
            }

            _logger.LogInformation("Parsed {RecordCount:N0} records from Excel file", wirelessValidationRecords.Count);
            processingResults.TotalRecords = wirelessValidationRecords.Count;

            // OPTIMIZED: Preload reference data (like Speedtest approach)
            var referenceData = await PreloadReferenceData(wirelessValidationRecords, token);
            _logger.LogInformation("Reference data preloaded successfully");

            // Resolve Municipality using Hierarchical Approach
            await ResolveMunicipalityHierarchical(wirelessValidationRecords, referenceData, token);

            // OPTIMIZED: Process all records in batches (like Speedtest)
            await ProcessRecordsInBatches(wirelessValidationRecords, referenceData, processingResults, token);

            await CompleteProcessing(file, processingResults, startTime, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wireless validation file with ID {FileId}", parameters.Id);
            await ProcessingJobHelper.TerminateProcessAsync(file, _fileRepository, _logger, FileTypeEnum.WirelessValidation, cancellationToken: token);
            throw;
        }
    }

    // OPTIMIZED: Like Speedtest - preload only essential fields for duplicate checking
    private async Task<ReferenceData> PreloadReferenceData(List<WirelessValidationRecord> records, CancellationToken token)
    {
        _logger.LogInformation("Preloading reference data with optimized approach...");

        // Preload wireless service providers
        var serviceProviderCache = await _serviceProviderRepository
            .Query(sp => sp.Service.Type == ServiceTypeEnum.Wireless)
            .Select(sp => new { sp.Id, sp.Provider.Name })
            .ToDictionaryAsync(
                sp => CacheKeyGenerator.GenerateServiceProviderCacheKey(sp.Name, ServiceTypeEnum.Wireless),
                sp => sp.Id,
                token
            );
        _logger.LogInformation("Preloaded {Count:N0} wireless service providers", serviceProviderCache.Count);

        // Preload hierarchical municipality data
        var municipalityHierarchyList = await _municipalityRepository
            .Query()
            .Select(m => new {
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
        _logger.LogInformation("Preloaded {Count:N0} municipalities with hierarchical data", municipalityHierarchyCache.Count);

        // Preload technologies
        var technologyCache = await _technologyRepository
            .Query()
            .Select(t => new { t.Id, Technology = t.Standard })
            .ToDictionaryAsync(
                t => CacheKeyGenerator.GenerateTechnologyCacheKey(t.Technology),
                t => t.Id,
                token
            );
        _logger.LogInformation("Preloaded {Count:N0} technologies", technologyCache.Count);
        TechnologyMappingHelper.LogTechnologyDiagnostics(technologyCache, _logger);

        // Preload bands
        var bandCache = await _bandRepository
            .Query()
            .Select(b => new { b.Id, Band = b.Name })
            .ToDictionaryAsync(
                b => CacheKeyGenerator.GenerateBandCacheKey(b.Band),
                b => b.Id,
                token
            );
        _logger.LogInformation("Preloaded {Count:N0} bands", bandCache.Count);

        // OPTIMIZED: Like Speedtest - preload ONLY essential fields for duplicate checking
        var serviceProviderIds = serviceProviderCache.Values.ToList();
        var existingValidations = await _wirelessValidationRepository
            .Query(wv => serviceProviderIds.Contains(wv.ServiceProviderId))
            .Select(wv => new {
                wv.ServiceProviderId,
                wv.BarangayId,
                wv.BandId,
                wv.TechnologyId,
                wv.Address
            })
            .ToListAsync(token);

        // OPTIMIZED: Simple HashSet for duplicate detection (like Speedtest)
        var validationCache = new HashSet<string>();
        var duplicateCount = 0;

        foreach (var validation in existingValidations)
        {
            var key = CacheKeyGenerator.GenerateWirelessValidationCacheKey(
                validation.ServiceProviderId,
                validation.BarangayId,
                validation.BandId,
                validation.TechnologyId,
                validation.Address ?? string.Empty
            );

            if (!validationCache.Add(key))
            {
                duplicateCount++;
                _logger.LogDebug("Duplicate wireless validation found in database: {Key}", key);
            }
        }

        if (duplicateCount > 0)
        {
            _logger.LogWarning("Found {DuplicateCount} duplicate wireless validation records in database. " +
                "Consider cleaning up duplicate validation data.", duplicateCount);
        }

        _logger.LogInformation("Preloaded {Count:N0} existing wireless validation records ({DuplicateCount} duplicates found, {TotalValidations} total)",
            validationCache.Count, duplicateCount, existingValidations.Count);

        return new ReferenceData
        {
            ServiceProviderCache = serviceProviderCache,
            MunicipalityCache = municipalityCache,
            MunicipalityHierarchyCache = municipalityHierarchyCache,
            TechnologyCache = technologyCache,
            BandCache = bandCache,
            WirelessValidationCache = validationCache
        };
    }

    private async Task ResolveMunicipalityHierarchical(List<WirelessValidationRecord> records, ReferenceData referenceData, CancellationToken token)
    {
        var municipalityGroups = records.GroupBy(r => new { r.Municipality, r.Province, r.Region }).ToList();

        foreach (var group in municipalityGroups)
        {
            var municipalityName = group.Key.Municipality;
            var provinceName = group.Key.Province;
            var regionName = group.Key.Region;

            MunicipalityHierarchyInfo? resolvedMunicipality = null;

            var hierarchicalKey = GenerateHierarchicalMunicipalityKey(municipalityName, provinceName, regionName);
            if (referenceData.MunicipalityHierarchyCache.TryGetValue(hierarchicalKey, out resolvedMunicipality))
            {
                _logger.LogInformation("Exact hierarchical match found for: {Municipality}, {Province}, {Region}",
                    municipalityName, provinceName, regionName);
            }
            else
            {
                resolvedMunicipality = await FindMunicipalityWithFuzzyMatching(municipalityName, provinceName, regionName, referenceData, token);
            }

            if (resolvedMunicipality != null)
            {
                // Load barangays for this municipality
                if (!referenceData.BarangayCache.ContainsKey(resolvedMunicipality.Id))
                {
                    var barangayList = await _barangayRepository
                        .Query(b => b.MunicipalityId == resolvedMunicipality.Id)
                        .Select(b => new { b.Id, b.Name, b.MunicipalityId })
                        .ToListAsync(token);

                    var barangayCache = new Dictionary<string, Guid>();
                    foreach (var barangay in barangayList)
                    {
                        var key = CacheKeyGenerator.GenerateBarangayCacheKey(barangay.Name, barangay.MunicipalityId);
                        barangayCache[key] = barangay.Id;
                    }

                    referenceData.BarangayCache[resolvedMunicipality.Id] = barangayCache;
                    _logger.LogInformation("Preloaded {Count:N0} barangays for municipality: {Municipality}",
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
                _logger.LogInformation("Fuzzy match candidate: {Municipality} ({MScore:F2}), {Province} ({PScore:F2}), {Region} ({RScore:F2})",
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
                .Select(m => new {
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

    // OPTIMIZED: Single-pass processing like Speedtest
    private async Task ProcessRecordsInBatches(List<WirelessValidationRecord> records, ReferenceData referenceData,
        ProcessingResults results, CancellationToken token)
    {
        var totalBatches = (int)Math.Ceiling((double)records.Count / ProcessingBatchSize);
        var validationsToAdd = new List<WirelessValidation>();

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

        _logger.LogInformation("Processing {RecordCount:N0} records in {BatchCount} batches", records.Count, totalBatches);

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = records.Skip(batchIndex * ProcessingBatchSize).Take(ProcessingBatchSize).ToList();

            foreach (var record in batch)
            {
                try
                {
                    results.ProcessedRecords++;

                    if (!record.MunicipalityId.HasValue || string.IsNullOrWhiteSpace(record.Provider))
                    {
                        results.SkippedRecords++;
                        continue;
                    }

                    var serviceProviderKey = CacheKeyGenerator.GenerateServiceProviderCacheKey(record.Provider, ServiceTypeEnum.Wireless);
                    if (!referenceData.ServiceProviderCache.TryGetValue(serviceProviderKey, out var serviceProviderId))
                    {
                        _logger.LogWarning("Service Provider not found: {Provider}. Skipping record.", record.Provider);
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
                        _logger.LogWarning("Skipping record without resolved barangay: Provider={Provider}, Barangay='{Barangay}', Municipality={Municipality}",
                            record.Provider, record.Barangay, record.Municipality);
                        results.SkippedRecords++;
                        continue;
                    }

                    // Process each technology/band combination
                    var (recordValidations, skippedDuplicates) = ProcessTechnologyBandCombinations(record, bandTechnologyProperties, referenceData);
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
                    _logger.LogWarning(ex, "Error processing record - Provider: {Provider}, Barangay: {Barangay}, Municipality: {Municipality}",
                        record.Provider, record.Barangay, record.Municipality);
                    results.ErrorRecords++;
                }
            }

            // Progress reporting
            var progress = (double)(batchIndex + 1) / totalBatches * 100;
            ProcessingJobHelper.ReportProgress($"Batch {batchIndex + 1}/{totalBatches} completed - Progress: {progress:F1}% - " +
                $"Successful: {results.SuccessfulRecords:N0}, Errors: {results.ErrorRecords:N0}, Validations Created: {results.CreatedValidations:N0}, Duplicates Skipped: {results.SkippedDuplicates:N0}", _logger);
        }

        // OPTIMIZED: Single batch insert at the end (like Speedtest)
        if (validationsToAdd.Count != 0)
        {
            _logger.LogInformation("Saving {Count:N0} wireless validations to database...", validationsToAdd.Count);
            _wirelessValidationRepository.AddRange(validationsToAdd);
            await _wirelessValidationRepository.SaveChangesAsync(token);
            _logger.LogInformation("Successfully saved all wireless validations");
        }
        else
        {
            _logger.LogInformation("No wireless validations to save");
        }
    }

    // OPTIMIZED: In-memory duplicate checking only (no database queries per record)
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
                    _logger.LogWarning("Skipping technology/band combination due to unresolved technology: {Technology}/{Band}",
                        item.Technology, item.Band);
                    continue;
                }

                var bandId = TechnologyMappingHelper.ResolveBandId(item.Band, referenceData.BandCache, _logger, contextInfo);

                if (!TechnologyMappingHelper.IsBandResolved(bandId))
                {
                    _logger.LogWarning("Skipping technology/band combination due to unresolved band: {Technology}/{Band}",
                        item.Technology, item.Band);
                    continue;
                }

                // OPTIMIZED: Simple in-memory duplicate check (like Speedtest)
                var validationCacheKey = CacheKeyGenerator.GenerateWirelessValidationCacheKey(
                    record.ServiceProviderId!.Value,
                    record.BarangayId!.Value,
                    bandId,
                    technologyId,
                    record.Address ?? string.Empty
                );

                if (referenceData.WirelessValidationCache.Contains(validationCacheKey))
                {
                    skippedDuplicates++;
                    _logger.LogDebug("Skipping duplicate wireless validation - Provider: {Provider}, Technology: {Technology}, Band: {Band}",
                        record.Provider, item.Technology, item.Band);
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
                referenceData.WirelessValidationCache.Add(validationCacheKey);

                var qualityDesc = rsrpValue.HasValue
                    ? RSRPParser.GetSignalQualityDescription(rsrpValue.Value, item.Technology)
                    : (isMarginal ? "Marginal (No RSRP)" : "Good (No RSRP)");

                _logger.LogDebug("Created validation - Provider: {Provider}, Technology: {Technology}, Band: {Band}, " +
                    "RSRP: {RSRP}, Marginal: {IsMarginal}, Quality: {Quality}",
                    record.Provider, item.Technology, item.Band,
                    rsrpValue?.ToString() ?? "NULL", isMarginal, qualityDesc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing technology/band combination: {Technology}/{Band} for record {Provider}",
                    item.Technology, item.Band, record.Provider);
            }
        }

        return (validations, skippedDuplicates);
    }

    private async Task CompleteProcessing(Domain.Entities.File file, ProcessingResults results, DateTime startTime, CancellationToken token)
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
            
            // Additional statistics
            var avgValidationsPerRecord = results.SuccessfulRecords > 0 
                ? (double)results.CreatedValidations / results.SuccessfulRecords 
                : 0;
            _logger.LogInformation("Average Validations per Record: {AvgValidations:F1}", avgValidationsPerRecord);
            
            // Duplicate statistics
            var totalPotentialValidations = results.CreatedValidations + results.SkippedDuplicates;
            var duplicateRate = totalPotentialValidations > 0 
                ? (double)results.SkippedDuplicates / totalPotentialValidations * 100 
                : 0;
            _logger.LogInformation("Duplicate Rate: {DuplicateRate:F1}% ({SkippedDuplicates:N0} of {TotalPotential:N0} potential validations)", 
                duplicateRate, results.SkippedDuplicates, totalPotentialValidations);
        }
        
        _logger.LogInformation("Processing Logic Applied:");
        _logger.LogInformation("- Hierarchical municipality resolution with Province and Region validation");
        _logger.LogInformation("- Fuzzy matching for address variations and spelling differences");
        _logger.LogInformation("- Barangay resolution is REQUIRED - records without resolved barangay are skipped");
        _logger.LogInformation("- Consistent address construction from all available location components");
        _logger.LogInformation("- Duplicate validation detection and prevention");
        _logger.LogInformation("- 'false', empty, N/A values: Skipped (no entity created)");
        _logger.LogInformation("- 'marginal' values: Created with IsMarginal=true, RSRP=NULL");
        _logger.LogInformation("- 'true' values: Created with IsMarginal=false, RSRP=NULL");
        _logger.LogInformation("- RSRP values: Parsed and evaluated per technology thresholds");
        _logger.LogInformation("- NBC Thresholds: 2G/3G <= -105dBm, 4G/5G <= -120dBm (not created)");
        _logger.LogInformation("- Marginal Thresholds: 2G/3G <= -90dBm, 4G/5G <= -100dBm");
        _logger.LogInformation("- Duplicate Detection: ServiceProvider + Barangay + Band + Technology + Address combination");
        _logger.LogInformation("=======================================");

        // Update file status
        file.Status = results.ErrorRecords == 0 ? FileStatusEnum.Success : FileStatusEnum.PartiallyProcessed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        _logger.LogInformation("Processing completed successfully.");
    }

    // Supporting classes
    private class ReferenceData
    {
        public Dictionary<string, Guid> ServiceProviderCache { get; set; } = new();
        public Dictionary<string, Guid> MunicipalityCache { get; set; } = new();
        public Dictionary<string, MunicipalityHierarchyInfo> MunicipalityHierarchyCache { get; set; } = new();
        public Dictionary<string, Guid> TechnologyCache { get; set; } = new();
        public Dictionary<string, Guid> BandCache { get; set; } = new();
        public Dictionary<Guid, Dictionary<string, Guid>> BarangayCache { get; set; } = new();
        public HashSet<string> WirelessValidationCache { get; set; } = new();
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
