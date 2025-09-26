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
                                          IWirelessValidationRepository wirelessValidationRepository
                                          ) : IProcessWirelessValidationJob
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly ILogger<ProcessWirelessValidationJob> _logger = logger;
    private readonly IRegionRepository _regionRepository = regionRepository;
    private readonly IProvinceRepository _provinceRepository = provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly IWirelessValidationRepository _wirelessValidationRepository = wirelessValidationRepository;

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
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

        var processingResults = new ProcessingResults();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting wireless validation processing for file: {FileName}", file.Name);

            // Phase 1: Parse Excel File
            using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
            var wirelessValidationRecords = WirelessValidationExcelHelper.ParseExcelToWirelessValidationRecords(fileStream);
            
            if (wirelessValidationRecords.Count == 0)
            {
                _logger.LogInformation("The Excel file is empty or contains no valid data.");
                await CompleteProcessing(file, processingResults, startTime, token);
                return;
            }

            _logger.LogInformation("Parsed {RecordCount:N0} records from Excel file", wirelessValidationRecords.Count);
            processingResults.TotalRecords = wirelessValidationRecords.Count;

            // Phase 2: Preload Reference Data with Hierarchical Structure
            var referenceData = await PreloadReferenceData(wirelessValidationRecords, token);
            _logger.LogInformation("Reference data preloaded successfully");

            // Phase 3: Resolve Municipality using Hierarchical Approach
            await ResolveMunicipalityHierarchical(wirelessValidationRecords, referenceData, token);

            // Phase 4: Process Records in Batches
            await ProcessRecordsInBatches(wirelessValidationRecords, referenceData, processingResults, token);

            // Phase 5: Complete Processing
            await CompleteProcessing(file, processingResults, startTime, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wireless validation file with ID {FileId}", parameters.Id);
            file.Status = FileStatusEnum.Failed;
            _fileRepository.Update(file);
            await _fileRepository.SaveChangesAsync(token);
            throw; 
        }
    }

    private async Task<ReferenceData> PreloadReferenceData(List<WirelessValidationRecord> records, CancellationToken token)
    {
        _logger.LogInformation("Preloading reference data with hierarchical structure...");

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

        // Preload hierarchical municipality data with Province and Region information
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

        // Also create a simple municipality cache for fallback lookup
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
        var technologyList = await _technologyRepository
            .Query()
            .Select(t => new { t.Id, Technology = t.Standard })
            .ToListAsync(token);

        var technologyCache = new Dictionary<string, Guid>();
        foreach (var tech in technologyList)
        {
            var key = CacheKeyGenerator.GenerateTechnologyCacheKey(tech.Technology);
            technologyCache[key] = tech.Id;
        }

        _logger.LogInformation("Preloaded {Count:N0} technologies", technologyCache.Count);

        // Log technology diagnostics for debugging
        TechnologyMappingHelper.LogTechnologyDiagnostics(technologyCache, _logger);

        // Preload bands
        var bandList = await _bandRepository
            .Query()
            .Select(b => new { b.Id, Band = b.Name })
            .ToListAsync(token);

        var bandCache = new Dictionary<string, Guid>();
        foreach (var band in bandList)
        {
            var key = CacheKeyGenerator.GenerateBandCacheKey(band.Band);
            bandCache[key] = band.Id;
        }

        _logger.LogInformation("Preloaded {Count:N0} bands", bandCache.Count);

        // Preload existing wireless validation records for duplication checking
        var serviceProviderIds = serviceProviderCache.Values.ToList();
        var existingValidations = await _wirelessValidationRepository
            .Query(wv => serviceProviderIds.Contains(wv.ServiceProviderId))
            .ToListAsync(token);

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
                _logger.LogWarning("Duplicate wireless validation found in database: ServiceProvider={ServiceProviderId}, " +
                    "Barangay={BarangayId}, Band={BandId}, Technology={TechnologyId}, Address='{Address}'. " +
                    "Consider cleaning up duplicate validation data.",
                    validation.ServiceProviderId, validation.BarangayId, validation.BandId, 
                    validation.TechnologyId, validation.Address);
            }
        }

        _logger.LogInformation("Preloaded {Count:N0} existing wireless validation records ({DuplicateCount} duplicates found in database, {TotalValidations} total)",
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
        // Group records by their hierarchical municipality information
        var municipalityGroups = records.GroupBy(r => new { r.Municipality, r.Province, r.Region }).ToList();
        
        foreach (var group in municipalityGroups)
        {
            var municipalityName = group.Key.Municipality;
            var provinceName = group.Key.Province;
            var regionName = group.Key.Region;
            
            MunicipalityHierarchyInfo? resolvedMunicipality = null;

            // Option A: Hierarchical matching (preferred approach)
            var hierarchicalKey = GenerateHierarchicalMunicipalityKey(municipalityName, provinceName, regionName);
            if (referenceData.MunicipalityHierarchyCache.TryGetValue(hierarchicalKey, out resolvedMunicipality))
            {
                _logger.LogInformation("Exact hierarchical match found for: {Municipality}, {Province}, {Region}", 
                    municipalityName, provinceName, regionName);
            }
            else
            {
                // Option B: Fuzzy matching with hierarchical validation
                resolvedMunicipality = await FindMunicipalityWithFuzzyMatching(municipalityName, provinceName, regionName, referenceData, token);
            }

            if (resolvedMunicipality != null)
            {
                // Preload barangays for this municipality
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

                // Set municipality ID for all records in this group
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

    private async Task<MunicipalityHierarchyInfo?> FindMunicipalityWithFuzzyMatching(string municipalityName, string provinceName, string regionName, ReferenceData referenceData, CancellationToken token)
    {
        _logger.LogInformation("Attempting fuzzy match for: {Municipality}, {Province}, {Region}", municipalityName, provinceName, regionName);

        // Try different variations and tolerance levels
        var possibleMatches = new List<MunicipalityHierarchyInfo>();

        foreach (var kvp in referenceData.MunicipalityHierarchyCache)
        {
            var municipalityInfo = kvp.Value;
            
            // Calculate similarity scores
            var municipalityScore = CalculateSimilarity(municipalityName, municipalityInfo.Name);
            var provinceScore = CalculateSimilarity(provinceName, municipalityInfo.ProvinceName);
            var regionScore = CalculateSimilarity(regionName, municipalityInfo.RegionName);

            // Require high municipality match, moderate province match
            if (municipalityScore >= 0.8 && provinceScore >= 0.6 && regionScore >= 0.5)
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
            return possibleMatches[0];
        }
        else if (possibleMatches.Count > 1)
        {
            // Select the best match based on combined score
            var bestMatch = possibleMatches
                .Select(m => new {
                    Municipality = m,
                    Score = (CalculateSimilarity(municipalityName, m.Name) * 2) + // Weight municipality name more heavily
                           CalculateSimilarity(provinceName, m.ProvinceName) +
                           CalculateSimilarity(regionName, m.RegionName)
                })
                .OrderByDescending(x => x.Score)
                .First();
            
            _logger.LogInformation("Best fuzzy match selected: {Municipality}, {Province}, {Region} (Score: {Score:F2})", 
                bestMatch.Municipality.Name, bestMatch.Municipality.ProvinceName, bestMatch.Municipality.RegionName, bestMatch.Score);
            return bestMatch.Municipality;
        }

        _logger.LogWarning("No suitable fuzzy match found for: {Municipality}, {Province}, {Region}", municipalityName, provinceName, regionName);
        return null;
    }

    private static double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0;

        source = source.ToLowerInvariant().Trim();
        target = target.ToLowerInvariant().Trim();

        if (source == target)
            return 1.0;

        // Check if one contains the other
        if (source.Contains(target) || target.Contains(source))
            return 0.9;

        // Levenshtein distance calculation
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

    private async Task ProcessRecordsInBatches(List<WirelessValidationRecord> records, ReferenceData referenceData, 
        ProcessingResults results, CancellationToken token)
    {
        const int batchSize = 2000; // Smaller batches for wireless validation due to multiple entities per record
        var totalBatches = (int)Math.Ceiling((double)records.Count / batchSize);

        _logger.LogInformation("Processing {RecordCount:N0} records in {BatchCount} batches", records.Count, totalBatches);

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = records.Skip(batchIndex * batchSize).Take(batchSize).ToList();
            var batchResults = await ProcessBatch(batch, referenceData, token);

            // Merge batch results
            results.SuccessfulRecords += batchResults.SuccessfulRecords;
            results.ErrorRecords += batchResults.ErrorRecords;
            results.SkippedRecords += batchResults.SkippedRecords;
            results.CreatedValidations += batchResults.CreatedValidations;
            results.SkippedDuplicates += batchResults.SkippedDuplicates;
            results.ProcessedRecords += batch.Count;

            // Progress reporting
            var progress = (double)(batchIndex + 1) / totalBatches * 100;
            _logger.LogInformation("Batch {BatchNumber}/{TotalBatches} completed - Progress: {Progress:F1}% - " +
                "Successful: {Successful:N0}, Errors: {Errors:N0}, Validations Created: {Created:N0}, Duplicates Skipped: {Duplicates:N0}",
                batchIndex + 1, totalBatches, progress, 
                batchResults.SuccessfulRecords, batchResults.ErrorRecords, batchResults.CreatedValidations, batchResults.SkippedDuplicates);
        }
    }

    private async Task<ProcessingResults> ProcessBatch(List<WirelessValidationRecord> batch, ReferenceData referenceData, CancellationToken token)
    {
        var results = new ProcessingResults();
        var validationsToCreate = new List<WirelessValidation>();

        // Get band/technology properties for processing
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

        foreach (var record in batch)
        {
            try
            {
                // Validate municipality
                if (!record.MunicipalityId.HasValue)
                {
                    results.SkippedRecords++;
                    continue;
                }

                // Validate and resolve service provider
                if (string.IsNullOrWhiteSpace(record.Provider))
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

                // Validate and resolve barangay
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

                // Process each technology/band combination
                var (recordValidations, skippedDuplicates) = ProcessTechnologyBandCombinations(record, bandTechnologyProperties, referenceData);
                validationsToCreate.AddRange(recordValidations);
                results.SkippedDuplicates += skippedDuplicates;

                if (recordValidations.Any())
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

        // Save all validations in a single batch operation
        if (validationsToCreate.Any())
        {
            _wirelessValidationRepository.AddRange(validationsToCreate);
            await _wirelessValidationRepository.SaveChangesAsync(token);
        }

        return results;
    }

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

                // Fix: Ensure propertyValue is not null before passing to ParseWirelessData
                var (shouldCreate, rssiValue, isMarginal) = RSSIParser.ParseWirelessData(propertyValue ?? string.Empty, item.Technology);

                // Skip if we shouldn't create an entity (false, empty, NBC, etc.)
                if (!shouldCreate)
                    continue;

                // Use TechnologyMappingHelper to resolve technology ID with fallbacks
                var contextInfo = $"Provider: {record.Provider}, Municipality: {record.Municipality}, Technology: {item.Technology}, Band: {item.Band}";
                var technologyId = TechnologyMappingHelper.ResolveTechnologyId(item.Technology, referenceData.TechnologyCache, _logger, contextInfo);
                
                if (!TechnologyMappingHelper.IsTechnologyResolved(technologyId))
                {
                    _logger.LogWarning("Skipping technology/band combination due to unresolved technology: {Technology}/{Band}", 
                        item.Technology, item.Band);
                    continue;
                }

                // Use TechnologyMappingHelper to resolve band ID
                var bandId = TechnologyMappingHelper.ResolveBandId(item.Band, referenceData.BandCache, _logger, contextInfo);
                
                if (!TechnologyMappingHelper.IsTechnologyResolved(bandId)) // Reusing the same check since it's just checking for Guid.Empty
                {
                    _logger.LogWarning("Skipping technology/band combination due to unresolved band: {Technology}/{Band}", 
                        item.Technology, item.Band);
                    continue;
                }

                // Check for duplicate wireless validation record
                var validationCacheKey = CacheKeyGenerator.GenerateWirelessValidationCacheKey(
                    record.ServiceProviderId!.Value,
                    record.BarangayId,
                    bandId,
                    technologyId,
                    record.Address ?? string.Empty
                );

                if (referenceData.WirelessValidationCache.Contains(validationCacheKey))
                {
                    skippedDuplicates++;
                    _logger.LogDebug("Skipping duplicate wireless validation - Provider: {Provider}, Technology: {Technology}, Band: {Band}, " +
                        "Barangay: {Barangay}, Address: {Address}. Record already exists in database.",
                        record.Provider, item.Technology, item.Band, record.Barangay, record.Address);
                    continue;
                }

                // Create wireless validation entity
                var validation = new WirelessValidation
                {
                    ServiceProviderId = record.ServiceProviderId!.Value,
                    BarangayId = record.BarangayId,
                    BandId = bandId,
                    TechnologyId = technologyId,
                    Address = record.Address, // Complete address: "Barangay, Municipality, Province, Region"
                    RSSI = rssiValue ?? 0, // Use parsed RSSI or 0 if null (for true/marginal cases)
                    IsMarginal = isMarginal
                };

                validations.Add(validation);

                // Add to cache to prevent duplicates within the same batch
                referenceData.WirelessValidationCache.Add(validationCacheKey);

                // Log detailed information for debugging
                var qualityDesc = rssiValue.HasValue 
                    ? RSSIParser.GetSignalQualityDescription(rssiValue.Value, item.Technology)
                    : (isMarginal ? "Marginal (No RSSI)" : "Good (No RSSI)");
                
                _logger.LogDebug("Created validation - Provider: {Provider}, Technology: {Technology}, Band: {Band}, " +
                    "RSSI: {RSSI}, Marginal: {IsMarginal}, Quality: {Quality}", 
                    record.Provider, item.Technology, item.Band, 
                    rssiValue?.ToString() ?? "N/A", isMarginal, qualityDesc);
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
        _logger.LogInformation("- Duplicate validation detection and prevention");
        _logger.LogInformation("- 'false', empty, N/A values: Skipped (no entity created)");
        _logger.LogInformation("- 'marginal' values: Created with IsMarginal=true, RSSI=0");
        _logger.LogInformation("- 'true' values: Created with IsMarginal=false, RSSI=0");
        _logger.LogInformation("- RSSI values: Parsed and evaluated per technology thresholds");
        _logger.LogInformation("- NBC Thresholds: 2G/3G <= -105dBm, 4G/5G <= -120dBm (not created)");
        _logger.LogInformation("- Marginal Thresholds: 2G/3G <= -90dBm, 4G/5G <= -100dBm");
        _logger.LogInformation("- Duplicate Detection: ServiceProvider + Barangay + Band + Technology + Address combination");
        _logger.LogInformation("=======================================");

        // Update file status
        file.Status = results.ErrorRecords == 0 ? FileStatusEnum.Success : FileStatusEnum.PartiallyProcessed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);
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
