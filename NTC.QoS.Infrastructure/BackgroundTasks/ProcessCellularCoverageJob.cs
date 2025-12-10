using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessCellularCoverageJob : IProcessCellularCoverageJob
{
    private readonly ILogger<ProcessCellularCoverageJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.Network> _networkRepository;
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Domain.Entities.CellularCoverage> _cellularCoverageRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICellularCoverageMappingService _mappingService;

    public ProcessCellularCoverageJob(
        ILogger<ProcessCellularCoverageJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Network> networkRepository,
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.CellularCoverage> cellularCoverageRepository,
        IBlobStorageService blobStorageService,
        ICellularCoverageMappingService mappingService
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _networkRepository = networkRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
        _blobStorageService = blobStorageService;
        _mappingService = mappingService;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var file = await ProcessingJobHelper.ValidateAndGetFileAsync(
            parameters.Id, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);

        await ProcessingJobHelper.SetFileProcessingStatusAsync(
            file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);

        try
        {
            var cellularCoverageList = new List<Domain.Entities.CellularCoverage>();

            _logger.LogInformation("Pre-loading reference data.");

            // Initialize mapping service (pre-load CellSectors and Municipalities)
            await _mappingService.InitializeAsync(token);

            // Pre-load WIRELESS service providers only (handle duplicates by grouping)
            var serviceProviders = await _serviceProviderRepository
                .Query(sp => !sp.IsDeleted)
                .Include(sp => sp.Service)
                .Where(sp => sp.Service.Type == ServiceTypeEnum.Wireless)
                .Select(sp => new { sp.Id, sp.Provider.Name })
                .ToListAsync(token);

            var serviceProviderCache = serviceProviders
                .GroupBy(sp => CacheKeyGenerator.GenerateSimpleNetworkCacheKey(sp.Name))
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Id  // Take first if duplicates exist
                );
            _logger.LogInformation("Pre-loaded {Count} wireless service providers.", serviceProviderCache.Count);

            // Pre-load networks with Band and Technology metadata
            // NOTE: Networks are cached by name only (not by ServiceProvider) because
            // we will filter by ServiceProvider during lookup to ensure correct network selection
            var networks = await _networkRepository
                .Query(n => !n.IsDeleted)
                .Include(n => n.SystemType)
                .ThenInclude(st => st.Technology)
                .Select(n => new { 
                    n.Id, 
                    n.Name, 
                    n.BandId,
                    TechnologyId = n.SystemType != null ? n.SystemType.TechnologyId : (Guid?)null,
                    ServiceProviderId = n.ServiceProviderId
                })
                .ToListAsync(token);

            _logger.LogInformation("Pre-loaded {Count} networks.", networks.Count);

            // Download file from blob storage or access local file
            Stream fileStream;
            try
            {
                fileStream = await ProcessingJobHelper.GetFileStreamAsync(
                    parameters.Path, _blobStorageService, _logger, FileTypeEnum.CellularCoverage, token);
            }
            catch (FileNotFoundException ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);
                return;
            }
            catch (Exception ex)
            {
                await ProcessingJobHelper.HandleFileAccessErrorAsync(
                    ex, parameters.Path, file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);
                return;
            }

            using (fileStream)
            {
                _logger.LogInformation("Processing Cellular Coverage file downloaded from blob storage");

                fileStream.Position = 0;
                _logger.LogInformation("Excel processing started.");

                var cellularCoverageRecords = ExcelHelper.ParseExcelToClass<CellularCoverageRecord>(fileStream);

                if (cellularCoverageRecords.Count is 0)
                {
                    _logger.LogInformation("The Excel is empty.");
                    await ProcessingJobHelper.SetFileSuccessStatusAsync(
                        file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);
                    return;
                }

                _logger.LogInformation("Processing {Count} records from Excel file", cellularCoverageRecords.Count);

                // Track statistics
                int processedCount = 0;
                int skippedDuplicates = 0;

                foreach (var record in cellularCoverageRecords)
                {
                    _logger.LogInformation("Processing record: {Record}", record);

                    ProcessingJobHelper.ReportProgress($"Processing record: SP={record.ServiceProvider}, Network={record.Network}, Lat={record.Latitude}, Lng={record.Longitude}, Strength={record.Strength}", _logger);

                    // Validate Service Provider
                    Guid? serviceProviderId = null;
                    if (!string.IsNullOrEmpty(record.ServiceProvider))
                    {
                        if (!serviceProviderCache.TryGetValue(
                            CacheKeyGenerator.GenerateSimpleNetworkCacheKey(record.ServiceProvider), 
                            out var spId))
                        {
                            _logger.LogWarning("Service Provider '{ServiceProvider}' not found", record.ServiceProvider);
                            await ProcessingJobHelper.TerminateProcessAsync(
                                file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                                $"Service Provider '{record.ServiceProvider}' not found in database", token);
                            return;
                        }
                        serviceProviderId = spId;
                    }

                    // Find network by name and ServiceProvider from pre-loaded list
                    var networkKey = CacheKeyGenerator.GenerateSimpleNetworkCacheKey(record.Network);
                    
                    // Filter networks: match by name first
                    var matchingNetworksByName = networks
                        .Where(n => CacheKeyGenerator.GenerateSimpleNetworkCacheKey(n.Name) == networkKey)
                        .ToList();

                    _logger.LogDebug(
                        "Network lookup: Name='{Network}' (normalized: '{NormalizedKey}'), Found {Count} networks with this name",
                        record.Network, networkKey, matchingNetworksByName.Count);

                    // If ServiceProvider is provided, filter by ServiceProvider
                    var matchingNetworks = matchingNetworksByName;
                    if (serviceProviderId.HasValue)
                    {
                        matchingNetworks = matchingNetworksByName
                            .Where(n => n.ServiceProviderId == serviceProviderId.Value)
                            .ToList();

                        _logger.LogDebug(
                            "After ServiceProvider filter (ServiceProviderId={ServiceProviderId}): {Count} networks match",
                            serviceProviderId.Value, matchingNetworks.Count);

                        if (matchingNetworks.Count == 0 && matchingNetworksByName.Count > 0)
                        {
                            // Show which ServiceProviders are available for this network
                            var availableProviders = matchingNetworksByName
                                .Select(n => n.ServiceProviderId)
                                .Distinct()
                                .ToList();
                            
                            _logger.LogWarning(
                                "Network '{Network}' exists but not for ServiceProvider '{ServiceProvider}' (Id: {ServiceProviderId}). Available ServiceProviderIds for this network: {AvailableProviders}",
                                record.Network, record.ServiceProvider, serviceProviderId.Value, string.Join(", ", availableProviders));
                        }
                    }

                    if (matchingNetworks.Count == 0)
                    {
                        var errorMsg = serviceProviderId.HasValue 
                            ? $"Network '{record.Network}' not found for Service Provider '{record.ServiceProvider}'. The network may not exist or does not belong to this service provider."
                            : $"Network '{record.Network}' not found in database";
                        
                        _logger.LogWarning(errorMsg);
                        await ProcessingJobHelper.TerminateProcessAsync(
                            file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                            errorMsg, token);
                        return;
                    }

                    if (matchingNetworks.Count > 1)
                    {
                        var errorMsg = $"Multiple networks found with name '{record.Network}' for ServiceProvider '{record.ServiceProvider ?? "N/A"}'. " +
                            "Please specify additional fields (e.g., Band or Technology) to disambiguate, or ensure network names are unique for the ServiceProvider.";
                        _logger.LogError(errorMsg);
                        await ProcessingJobHelper.TerminateProcessAsync(
                            file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                            errorMsg, token);
                        return;
                    }

                    var networkMetadata = matchingNetworks[0];
                    
                    _logger.LogDebug(
                        "Network matched: '{Network}' -> NetworkId={NetworkId}, ServiceProviderId={ServiceProviderId}", 
                        record.Network, networkMetadata.Id, networkMetadata.ServiceProviderId);

                    // Determine final ServiceProviderId: use user-provided if available, otherwise use Network's
                    var finalServiceProviderId = serviceProviderId ?? 
                        (networkMetadata.ServiceProviderId != Guid.Empty ? networkMetadata.ServiceProviderId : null);

                    // Map to infrastructure (CellSector and Municipality) using mapping service
                    var (cellSectorId, municipalityId) = await _mappingService.FindInfrastructureMappingAsync(
                        record.Latitude, record.Longitude);

                    if (cellSectorId == null)
                    {
                        _logger.LogDebug("No CellSector found for point ({Lat}, {Lng}). Record will be saved without infrastructure mapping.",
                            record.Latitude, record.Longitude);
                    }

                    // Check for duplicate: same NetworkId, BandId, and location within 1 meter
                    var pointToCheck = GeometryHelper.CreatePoint(new Coordinate(record.Longitude, record.Latitude));
                    const double spatialToleranceMeters = 1.0;
                    var isDuplicate = await _cellularCoverageRepository
                        .Query(cc => !cc.IsDeleted &&
                            cc.NetworkId == networkMetadata.Id &&
                            cc.BandId == networkMetadata.BandId &&
                            cc.Location.Distance(pointToCheck) <= spatialToleranceMeters)
                        .AnyAsync(token);

                    if (isDuplicate)
                    {
                        _logger.LogDebug("Skipping duplicate coverage point: Network={Network}, Lat={Lat}, Lng={Lng}", 
                            record.Network, record.Latitude, record.Longitude);
                        skippedDuplicates++;
                        continue;
                    }

                    // Create coverage record with all derived fields
                    var cellularCoverage = new Domain.Entities.CellularCoverage
                    {
                        NetworkId = networkMetadata.Id,
                        ServiceProviderId = finalServiceProviderId,
                        BandId = networkMetadata.BandId,
                        TechnologyId = networkMetadata.TechnologyId,
                        CellSectorId = cellSectorId,
                        MunicipalityId = municipalityId,
                        Location = GeometryHelper.CreatePoint(new Coordinate(x: record.Longitude, y: record.Latitude)),
                        Strength = record.Strength,
                        Address = record.Address,
                        IsFileUploaded = true,
                    };
                    cellularCoverageList.Add(cellularCoverage);
                    processedCount++;
                }

                if (cellularCoverageList.Count != 0)
                {
                    _cellularCoverageRepository.AddRange(cellularCoverageList);
                    await _cellularCoverageRepository.SaveChangesAsync(token);
                    _logger.LogInformation("{Count} cellular coverage records saved successfully.", cellularCoverageList.Count);
                }
                else
                {
                    _logger.LogInformation("No new cellular coverage records to add (all were duplicates).");
                }

                // Log processing summary
                _logger.LogInformation(
                    "Processing complete: Total={Total}, Processed={Processed}, Duplicates={Duplicates}",
                    cellularCoverageRecords.Count, processedCount, skippedDuplicates);

                await ProcessingJobHelper.SetFileSuccessStatusAsync(
                    file, _fileRepository, _logger, FileTypeEnum.CellularCoverage, token);

                _logger.LogInformation("File processing completed successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Cellular Coverage file.");
            await ProcessingJobHelper.TerminateProcessAsync(
                file, _fileRepository, _logger, FileTypeEnum.CellularCoverage,
                $"Error occurred while processing: {ex.Message}", token);
            throw;
        }
    }
}
