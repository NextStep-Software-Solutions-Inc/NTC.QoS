using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessNAPJob : IProcessNAPJob
{
    private readonly ILogger<ProcessNAPJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.NAP> _NAPRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;

    public ProcessNAPJob(
        ILogger<ProcessNAPJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.NAP> NAPRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _NAPRepository = NAPRepository;
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file is null)
        {
            var message = "File not found";
            _logger.LogError(message);
            throw new Exception(message);
        }

        file.Status = FileStatusEnum.Processing;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        _logger.LogInformation($"Processing file: {parameters.Path}");

        fileStream.Position = 0;
        _logger.LogInformation("Processing started");

        var napRecords = ExcelHelper.ParseExcelToClass<NAPRecord>(fileStream);

        if (napRecords.Count is 0)
        {
            _logger.LogInformation("The Excel is empty.");
            file.Status = FileStatusEnum.Success;
            _fileRepository.Update(file);
            await _fileRepository.SaveChangesAsync(token);
            return;
        }

        _logger.LogInformation($"Total records to process: {napRecords.Count}");

        // Preload all service providers
        var ProviderNames = napRecords.Select(r => r.Provider).Distinct().ToList();
        _logger.LogInformation($"Preloading {ProviderNames.Count} unique service providers from NAP data...");

        var serviceProvidersFromData = await _serviceProviderRepository
            .Query(sp => !sp.IsDeleted && sp.Provider != null)
            .Include(sp => sp.Provider)
            .ToListAsync(token);

        // Create lookup dictionary for exact matching
        var serviceProviderLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var sp in serviceProvidersFromData)
        {
            if (sp.Provider?.Name != null)
            {
                serviceProviderLookup[sp.Provider.Name] = sp.Id;
            }
        }

        _logger.LogInformation($"Preloaded {serviceProviderLookup.Count} service providers");

        var missingProviders = ProviderNames.Where(name => !serviceProviderLookup.ContainsKey(name)).ToList();
        if (missingProviders.Any())
        {
            _logger.LogWarning($"Missing service providers from database:");
            foreach (var missing in missingProviders)
            {
                _logger.LogWarning($"  - '{missing}'");
            }
            _logger.LogWarning($"Available service providers in database:");
            foreach (var available in serviceProviderLookup.Keys.Take(10))
            {
                _logger.LogWarning($"  - '{available}'");
            }
        }

        // Duplicate check - Preload existing NAPs for the providers in this file
        _logger.LogInformation("Preloading existing NAPs for duplicate checking...");
        
        var validProviderIds = serviceProviderLookup.Values.ToList();
        var existingNAPs = await _NAPRepository
            .ToListAsync(_NAPRepository.QueryAsNoTracking(nap => 
                !nap.IsDeleted && 
                validProviderIds.Contains(nap.ServiceProviderId)), token);

        // Create efficient lookup for existing NAPs
        var existingNAPKeys = existingNAPs
            .Select(n => $"{n.ServiceProviderId}|{n.Address}|{n.Location.X:F6}|{n.Location.Y:F6}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation($"Preloaded {existingNAPs.Count} existing NAP records for duplicate checking");

        // Batch processing
        const int batchSize = 5000;
        var totalBatches = (int)Math.Ceiling((double)napRecords.Count / batchSize);
        var totalProcessed = 0;
        var totalSuccessful = 0;
        var totalDuplicates = 0;

        var processedRecords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation($"Processing {napRecords.Count} records in {totalBatches} batches of {batchSize}");        
        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = napRecords.Skip(batchIndex * batchSize).Take(batchSize).ToList();
            var batchNAPs = new List<Domain.Aggregates.NAP>();

            _logger.LogInformation($"Processing batch {batchIndex + 1}/{totalBatches} ({batch.Count} records)");

            foreach (var record in batch)
            {
                totalProcessed++;

            try
            {
                if (!serviceProviderLookup.TryGetValue(record.Provider, out var serviceProviderId))
                {
                    _logger.LogWarning("Service Provider not found in preloaded data: {Provider}. Skipping record.", record.Provider);
                    continue;
                }

                // Validate coordinates
                if (record.Latitude < -90 || record.Latitude > 90 || 
                    record.Longitude < -180 || record.Longitude > 180)
                {
                    _logger.LogWarning($"Invalid coordinates for record: {record.Provider}, {record.Municipality} {record.Province} ({record.Latitude},{record.Longitude})");
                    continue;
                }

                var address = $"{record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";
                var location = GeometryHelper.CreatePoint(new Coordinate(y: record.Latitude, x: record.Longitude));

                var recordKey = $"{record.Provider}|{record.Barangay}|{record.Municipality}|{record.Region}|{record.Province}|{record.Latitude:F6}|{record.Longitude:F6}";
                var dbKey = $"{serviceProviderId}|{address}|{record.Longitude:F6}|{record.Latitude:F6}";
                
                // Check against existing database records
                if (existingNAPKeys.Contains(dbKey))
                {
                    totalDuplicates++;
                    _logger.LogInformation($"Duplicate skipped: {record.Provider}, {record.Municipality}, {record.Province} ({record.Latitude},{record.Longitude})");
                    continue;
                }
                
                // Check against records processed in this session
                if (processedRecords.Contains(recordKey))
                {
                    totalDuplicates++;
                    _logger.LogInformation($"Duplicate skipped: {record.Provider}, {record.Municipality}, {record.Province} ({record.Latitude},{record.Longitude})");
                    continue;
                }

                // Add to processed set to prevent future duplicates in this session
                processedRecords.Add(recordKey);
                // Also add to database keys to prevent duplicates in later batches
                existingNAPKeys.Add(dbKey);
                var nap = new Domain.Aggregates.NAP
                    {
                        ServiceProviderId = serviceProviderId,
                        Address = address,
                        Location = location,
                    };

                    batchNAPs.Add(nap);
                    totalSuccessful++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error processing record {totalProcessed}: {record.Provider}, {record.Municipality} {record.Province}. Error: {ex.Message}");
                    continue;
                }

                // Report progress every 1000 records
                if (totalProcessed % 1000 == 0)
                {
                    ReportProgress($"Processed {totalProcessed}/{napRecords.Count} records ({(double)totalProcessed / napRecords.Count * 100:F1}%)");
                }
            }

            // Batch save to database
            if (batchNAPs.Count > 0)
            {
                try
                {
                    await _NAPRepository.BulkInsertAsync(batchNAPs, token);
                    _logger.LogInformation($"Batch {batchIndex + 1}: Successfully bulk inserted {batchNAPs.Count} NAP records to database");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error bulk inserting batch {batchIndex + 1} to database: {ex.Message}");
                    await TerminateProcess(file, token);
                    return;
                }
            }

            ReportProgress($"Completed batch {batchIndex + 1}/{totalBatches}");
        }

        _logger.LogInformation($"Processing completed: {totalSuccessful}/{napRecords.Count} records successfully processed, {totalDuplicates} duplicates skipped");

        file.Status = FileStatusEnum.Success;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        _logger.LogInformation("Processing completed successfully");
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
        _logger.LogInformation("Process terminated due to error in runtime.");
    }
}

