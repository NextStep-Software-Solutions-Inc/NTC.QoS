using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed;
using NTC.QoS.Infrastructure.Seed.Parsers;
using NTC.QoS.Infrastructure.Seed.Strategies;
using NTC.QoS.Infrastructure.Seed.Transformers;
using NTC.QoS.Infrastructure.Seed.Writers;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job for processing location seeding operations
/// </summary>
[Queue("seed-processing")]
[AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class ProcessLocationSeedJob : IProcessLocationSeedJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ProcessLocationSeedJob> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;

    public ProcessLocationSeedJob(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ILogger<ProcessLocationSeedJob> logger,
        ILoggerFactory loggerFactory,
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
    }

    public async Task ExecuteAsync(LocationSeedParametersDto parameters, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Location seeding job started - Region: {Region}, Province: {Province}, Municipality: {Municipality}, Barangay: {Barangay}, ForceReseed: {ForceReseed}",
            parameters.RegionEntity, parameters.ProvinceEntity, parameters.MunicipalityEntity, parameters.BarangayEntity, parameters.ForceReseed);

        var results = new List<SeedingResult>();

        try
        {
            // Seed in hierarchical order
            if (parameters.RegionEntity)
            {
                var result = await SeedRegionsAsync(parameters.UseOptimizedSeeder, parameters.ForceReseed, cancellationToken);
                results.Add(new SeedingResult 
                { 
                    Entity = "Region", 
                    Success = result.success, 
                    Inserted = result.inserted,
                    Existing = result.existing,
                    AlreadySeeded = result.alreadySeeded,
                    Message = result.message 
                });
                _logger.LogInformation("Region seeding completed: Success={Success}, Inserted={Inserted}, Existing={Existing}", result.success, result.inserted, result.existing);
            }

            if (parameters.ProvinceEntity)
            {
                var result = await SeedProvincesAsync(parameters.UseOptimizedSeeder, parameters.ForceReseed, cancellationToken);
                results.Add(new SeedingResult 
                { 
                    Entity = "Province", 
                    Success = result.success, 
                    Inserted = result.inserted,
                    Existing = result.existing,
                    AlreadySeeded = result.alreadySeeded,
                    Message = result.message 
                });
                _logger.LogInformation("Province seeding completed: Success={Success}, Inserted={Inserted}, Existing={Existing}", result.success, result.inserted, result.existing);
            }

            if (parameters.MunicipalityEntity)
            {
                var result = await SeedMunicipalitiesAsync(parameters.UseOptimizedSeeder, parameters.ForceReseed, cancellationToken);
                results.Add(new SeedingResult 
                { 
                    Entity = "Municipality", 
                    Success = result.success, 
                    Inserted = result.inserted,
                    Existing = result.existing,
                    AlreadySeeded = result.alreadySeeded,
                    Message = result.message 
                });
                _logger.LogInformation("Municipality seeding completed: Success={Success}, Inserted={Inserted}, Existing={Existing}", result.success, result.inserted, result.existing);
            }

            if (parameters.BarangayEntity)
            {
                var result = await SeedBarangaysAsync(parameters.UseOptimizedSeeder, parameters.ForceReseed, cancellationToken);
                results.Add(new SeedingResult 
                { 
                    Entity = "Barangay", 
                    Success = result.success, 
                    Inserted = result.inserted,
                    Existing = result.existing,
                    AlreadySeeded = result.alreadySeeded,
                    Message = result.message 
                });
                _logger.LogInformation("Barangay seeding completed: Success={Success}, Inserted={Inserted}, Existing={Existing}", result.success, result.inserted, result.existing);
            }

            var summary = JsonConvert.SerializeObject(results);
            _logger.LogInformation("Location seeding job completed successfully. Summary: {Summary}", summary);

            // Return the results so Hangfire can store them
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Location seeding job failed");
            throw; // Hangfire will handle the retry logic
        }
    }

    private class SeedingResult
    {
        public string Entity { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int Inserted { get; set; }
        public int Existing { get; set; }
        public bool AlreadySeeded { get; set; }
        public string? Message { get; set; }
    }

    private async Task<(bool success, int inserted, int existing, bool alreadySeeded, string? message)> SeedRegionsAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
    {
        if (useOptimizedSeeder)
        {
            _logger.LogInformation("Using optimized LocationSeederV2 for regions");
            var parser = new StreamingGeoJsonParser(_loggerFactory.CreateLogger<StreamingGeoJsonParser>(), "ADM1", null);
            var transformer = new RegionTransformer();
            var writer = new BatchedLocationWriter<Domain.Entities.Region>(_dbContext, _loggerFactory.CreateLogger<BatchedLocationWriter<Domain.Entities.Region>>());
            var strategy = new RegionSeedingStrategy(_dbContext, _blobStorageService, _loggerFactory.CreateLogger<RegionSeedingStrategy>(), parser, transformer, writer);

            var result = await strategy.SeedAsync(null, forceReseed, cancellationToken);
            return (result.Success, result.InsertedCount, result.ExistingCount, result.AlreadySeeded, result.ErrorMessage);
        }
        else
        {
            _logger.LogInformation("Using legacy LocationSeeder for regions");
            await LocationSeeder.SeedRegionsAsync(_dbContext, _blobStorageService, _logger, forceReseed);
            return (true, 0, 0, false, "Legacy seeder completed");
        }
    }

    private async Task<(bool success, int inserted, int existing, bool alreadySeeded, string? message)> SeedProvincesAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
    {
        if (useOptimizedSeeder)
        {
            _logger.LogInformation("Using optimized LocationSeederV2 for provinces");

            // Load region ID map
            var regionsQuery = _regionRepository.QueryAsNoTracking(r => !r.IsDeleted);
            var regions = await _regionRepository.ToListAsync(
                regionsQuery.Select(r => new { r.Code, r.Id }),
                cancellationToken);
            var regionMap = regions.GroupBy(r => r.Code).ToDictionary(g => g.Key, g => g.First().Id);

            var parser = new StreamingGeoJsonParser(_loggerFactory.CreateLogger<StreamingGeoJsonParser>(), "ADM2", "ADM1");
            var transformer = new ProvinceTransformer();
            var writer = new BatchedLocationWriter<Domain.Entities.Province>(_dbContext, _loggerFactory.CreateLogger<BatchedLocationWriter<Domain.Entities.Province>>());
            var strategy = new ProvinceSeedingStrategy(_dbContext, _blobStorageService, _loggerFactory.CreateLogger<ProvinceSeedingStrategy>(), parser, transformer, writer);

            var result = await strategy.SeedAsync(regionMap, forceReseed, cancellationToken);
            return (result.Success, result.InsertedCount, result.ExistingCount, result.AlreadySeeded, result.ErrorMessage);
        }
        else
        {
            _logger.LogWarning("Legacy LocationSeeder does not support individual Province seeding. Use optimized seeder instead.");
            return (false, 0, 0, false, "Legacy seeder does not support individual province seeding");
        }
    }

    private async Task<(bool success, int inserted, int existing, bool alreadySeeded, string? message)> SeedMunicipalitiesAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
    {
        if (useOptimizedSeeder)
        {
            _logger.LogInformation("Using optimized LocationSeederV2 for municipalities");

            // Load province ID map
            var provincesQuery = _provinceRepository.QueryAsNoTracking(p => !p.IsDeleted);
            var provinces = await _provinceRepository.ToListAsync(
                provincesQuery.Select(p => new { p.Code, p.Id }),
                cancellationToken);
            var provinceMap = provinces.GroupBy(p => p.Code).ToDictionary(g => g.Key, g => g.First().Id);

            var parser = new StreamingGeoJsonParser(_loggerFactory.CreateLogger<StreamingGeoJsonParser>(), "ADM3", "ADM2");
            var transformer = new MunicipalityTransformer();
            var writer = new BatchedLocationWriter<Domain.Entities.Municipality>(_dbContext, _loggerFactory.CreateLogger<BatchedLocationWriter<Domain.Entities.Municipality>>());
            var strategy = new MunicipalitySeedingStrategy(_dbContext, _blobStorageService, _loggerFactory.CreateLogger<MunicipalitySeedingStrategy>(), parser, transformer, writer);

            var result = await strategy.SeedAsync(provinceMap, forceReseed, cancellationToken);
            return (result.Success, result.InsertedCount, result.ExistingCount, result.AlreadySeeded, result.ErrorMessage);
        }
        else
        {
            _logger.LogWarning("Legacy LocationSeeder does not support individual Municipality seeding. Use optimized seeder instead.");
            return (false, 0, 0, false, "Legacy seeder does not support individual municipality seeding");
        }
    }

    private async Task<(bool success, int inserted, int existing, bool alreadySeeded, string? message)> SeedBarangaysAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
    {
        if (useOptimizedSeeder)
        {
            _logger.LogInformation("Using optimized LocationSeederV2 for barangays");

            // Load municipality ID map
            var municipalitiesQuery = _municipalityRepository.QueryAsNoTracking(m => !m.IsDeleted);
            var municipalities = await _municipalityRepository.ToListAsync(
                municipalitiesQuery.Select(m => new { m.Code, m.Id }),
                cancellationToken);
            var municipalityMap = municipalities.GroupBy(m => m.Code).ToDictionary(g => g.Key, g => g.First().Id);

            var parser = new StreamingGeoJsonParser(_loggerFactory.CreateLogger<StreamingGeoJsonParser>(), "ADM4", "ADM3");
            var transformer = new BarangayTransformer();
            var writer = new BatchedLocationWriter<Domain.Entities.Barangay>(_dbContext, _loggerFactory.CreateLogger<BatchedLocationWriter<Domain.Entities.Barangay>>());
            var strategy = new BarangaySeedingStrategy(_dbContext, _blobStorageService, _loggerFactory.CreateLogger<BarangaySeedingStrategy>(), parser, transformer, writer);

            var result = await strategy.SeedAsync(municipalityMap, forceReseed, cancellationToken);
            return (result.Success, result.InsertedCount, result.ExistingCount, result.AlreadySeeded, result.ErrorMessage);
        }
        else
        {
            _logger.LogWarning("Legacy LocationSeeder does not support individual Barangay seeding. Use optimized seeder instead.");
            return (false, 0, 0, false, "Legacy seeder does not support individual barangay seeding");
        }
    }
}
