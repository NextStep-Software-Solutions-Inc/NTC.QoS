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

            // Phase 2: Preload Reference Data
            var referenceData = await PreloadReferenceData(wirelessValidationRecords, token);
            _logger.LogInformation("Reference data preloaded successfully");

            // Phase 3: Resolve Municipality for all records
            await ResolveMunicipality(wirelessValidationRecords, referenceData, token);

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
        _logger.LogInformation("Preloading reference data...");

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

        // Preload municipalities
        var municipalityList = await _municipalityRepository
            .Query()
            .Select(m => new { m.Id, m.Name })
            .ToListAsync(token);

        var municipalityCache = new Dictionary<string, Guid>();
        foreach (var municipality in municipalityList)
        {
            var key = CacheKeyGenerator.GenerateMunicipalityCacheKey(municipality.Name);
            municipalityCache[key] = municipality.Id;
        }

        _logger.LogInformation("Preloaded {Count:N0} municipalities", municipalityCache.Count);

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

        return new ReferenceData
        {
            ServiceProviderCache = serviceProviderCache,
            MunicipalityCache = municipalityCache,
            TechnologyCache = technologyCache,
            BandCache = bandCache
        };
    }

    private async Task ResolveMunicipality(List<WirelessValidationRecord> records, ReferenceData referenceData, CancellationToken token)
    {
        // Group records by municipality to minimize database queries
        var municipalityGroups = records.GroupBy(r => r.Municipality).ToList();
        
        foreach (var group in municipalityGroups)
        {
            var municipalityName = group.Key;
            var municipalityKey = CacheKeyGenerator.GenerateMunicipalityCacheKey(municipalityName);
            
            if (referenceData.MunicipalityCache.TryGetValue(municipalityKey, out var municipalityId))
            {
                // Preload barangays for this municipality
                if (!referenceData.BarangayCache.ContainsKey(municipalityId))
                {
                    var barangayList = await _barangayRepository
                        .Query(b => b.MunicipalityId == municipalityId)
                        .Select(b => new { b.Id, b.Name, b.MunicipalityId })
                        .ToListAsync(token);

                    var barangayCache = new Dictionary<string, Guid>();
                    foreach (var barangay in barangayList)
                    {
                        var key = CacheKeyGenerator.GenerateBarangayCacheKey(barangay.Name, barangay.MunicipalityId);
                        barangayCache[key] = barangay.Id;
                    }

                    referenceData.BarangayCache[municipalityId] = barangayCache;
                    _logger.LogInformation("Preloaded {Count:N0} barangays for municipality: {Municipality}", 
                        barangayCache.Count, municipalityName);
                }

                // Set municipality ID for all records in this group
                foreach (var record in group)
                {
                    record.MunicipalityId = municipalityId;
                }
            }
            else
            {
                _logger.LogWarning("Municipality not found: {Municipality}. {RecordCount} records will be skipped.", 
                    municipalityName, group.Count());
            }
        }
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
            results.ProcessedRecords += batch.Count;

            // Progress reporting
            var progress = (double)(batchIndex + 1) / totalBatches * 100;
            _logger.LogInformation("Batch {BatchNumber}/{TotalBatches} completed - Progress: {Progress:F1}% - " +
                "Successful: {Successful:N0}, Errors: {Errors:N0}, Validations Created: {Created:N0}",
                batchIndex + 1, totalBatches, progress, 
                batchResults.SuccessfulRecords, batchResults.ErrorRecords, batchResults.CreatedValidations);
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
                var recordValidations = ProcessTechnologyBandCombinations(record, bandTechnologyProperties, referenceData);
                validationsToCreate.AddRange(recordValidations);

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

    private List<WirelessValidation> ProcessTechnologyBandCombinations(WirelessValidationRecord record,
        List<BandTechnologyProperty> bandTechnologyProperties, ReferenceData referenceData)
    {
        var validations = new List<WirelessValidation>();

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

                // Get technology ID
                var technologyKey = CacheKeyGenerator.GenerateTechnologyCacheKey(item.Technology);
                if (!referenceData.TechnologyCache.TryGetValue(technologyKey, out Guid technologyId))
                {
                    _logger.LogWarning("Technology not found: {Technology} for property {PropertyName}", 
                        item.Technology, item.Property.Name);
                    continue;
                }

                // Get band ID
                var bandKey = CacheKeyGenerator.GenerateBandCacheKey(item.Band);
                if (!referenceData.BandCache.TryGetValue(bandKey, out Guid bandId))
                {
                    _logger.LogWarning("Band not found: {Band} for property {PropertyName}", 
                        item.Band, item.Property.Name);
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

        return validations;
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
        
        if (results.TotalRecords > 0)
        {
            var processingRate = results.TotalRecords / processingTime.TotalSeconds;
            _logger.LogInformation("Processing Rate: {ProcessingRate:F0} records/second", processingRate);
            
            // Additional statistics
            var avgValidationsPerRecord = results.SuccessfulRecords > 0 
                ? (double)results.CreatedValidations / results.SuccessfulRecords 
                : 0;
            _logger.LogInformation("Average Validations per Record: {AvgValidations:F1}", avgValidationsPerRecord);
        }
        
        _logger.LogInformation("Processing Logic Applied:");
        _logger.LogInformation("- 'false', empty, N/A values: Skipped (no entity created)");
        _logger.LogInformation("- 'marginal' values: Created with IsMarginal=true, RSSI=0");
        _logger.LogInformation("- 'true' values: Created with IsMarginal=false, RSSI=0");
        _logger.LogInformation("- RSSI values: Parsed and evaluated per technology thresholds");
        _logger.LogInformation("- NBC Thresholds: 2G/3G <= -105dBm, 4G/5G <= -120dBm (not created)");
        _logger.LogInformation("- Marginal Thresholds: 2G/3G <= -90dBm, 4G/5G <= -100dBm");
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
        public Dictionary<string, Guid> TechnologyCache { get; set; } = new();
        public Dictionary<string, Guid> BandCache { get; set; } = new();
        public Dictionary<Guid, Dictionary<string, Guid>> BarangayCache { get; set; } = new();
    }

    private class ProcessingResults
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int ErrorRecords { get; set; }
        public int CreatedValidations { get; set; }
    }

    private class BandTechnologyProperty
    {
        public PropertyInfo Property { get; set; } = null!;
        public ExcelHeaderAttribute? HeaderAttribute { get; set; }
        public string Band { get; set; } = string.Empty;
        public string Technology { get; set; } = string.Empty;
    }
}
