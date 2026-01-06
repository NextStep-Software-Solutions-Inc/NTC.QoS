using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NTC.QoS.API.Authorization;
using NTC.QoS.API.Models;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed;
using NTC.QoS.Infrastructure.Seed.Parsers;
using NTC.QoS.Infrastructure.Seed.Strategies;
using NTC.QoS.Infrastructure.Seed.Transformers;
using NTC.QoS.Infrastructure.Seed.Writers;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Controller for managing database seeding operations
/// </summary>
[ApiController]
[Route("seed", Name = "seed")]
[Authorize]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<SeedController> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DatabaseOptions _databaseOptions;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;

    public SeedController(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ILogger<SeedController> logger,
        ILoggerFactory loggerFactory,
        IOptions<DatabaseOptions> databaseOptions,
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _databaseOptions = databaseOptions.Value;
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
    }

    /// <summary>
    /// Reseed specific location entities based on the request payload.
    /// This endpoint allows selective reseeding without redeploying the system.
    /// </summary>
    /// <param name="request">The seed request containing which entities to reseed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the seeding operation</returns>
    [HttpPost]
    [RequirePermission(Permissions.SystemAdministration)]
    public async Task<IActionResult> ReseedEntities([FromBody] SeedRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Reseed request received - Region: {Region}, Province: {Province}, Municipality: {Municipality}, Barangay: {Barangay}, ForceReseed: {ForceReseed}",
            request.RegionEntity, request.ProvinceEntity, request.MunicipalityEntity, request.BarangayEntity, request.ForceReseed);

        if (!request.RegionEntity && !request.ProvinceEntity && !request.MunicipalityEntity && !request.BarangayEntity)
        {
            return BadRequest(new
            {
                success = false,
                message = "At least one entity type must be selected for reseeding.",
                timestamp = DateTime.UtcNow
            });
        }

        try
        {
            var results = new List<object>();
            var useOptimizedSeeder = _databaseOptions.UseOptimizedLocationSeeder ?? true;

            // Seed in hierarchical order
            if (request.RegionEntity)
            {
                var result = await SeedRegionsAsync(useOptimizedSeeder, request.ForceReseed, cancellationToken);
                results.Add(new { entity = "Region", result });
            }

            if (request.ProvinceEntity)
            {
                var result = await SeedProvincesAsync(useOptimizedSeeder, request.ForceReseed, cancellationToken);
                results.Add(new { entity = "Province", result });
            }

            if (request.MunicipalityEntity)
            {
                var result = await SeedMunicipalitiesAsync(useOptimizedSeeder, request.ForceReseed, cancellationToken);
                results.Add(new { entity = "Municipality", result });
            }

            if (request.BarangayEntity)
            {
                var result = await SeedBarangaysAsync(useOptimizedSeeder, request.ForceReseed, cancellationToken);
                results.Add(new { entity = "Barangay", result });
            }

            return Ok(new
            {
                success = true,
                message = "Reseeding completed successfully",
                results,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reseeding failed");
            return StatusCode(500, new
            {
                success = false,
                message = "Reseeding failed",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private async Task<object> SeedRegionsAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
    {
        if (useOptimizedSeeder)
        {
            _logger.LogInformation("Using optimized LocationSeederV2 for regions");
            var parser = new StreamingGeoJsonParser(_loggerFactory.CreateLogger<StreamingGeoJsonParser>(), "ADM1", null);
            var transformer = new RegionTransformer();
            var writer = new BatchedLocationWriter<Domain.Entities.Region>(_dbContext, _loggerFactory.CreateLogger<BatchedLocationWriter<Domain.Entities.Region>>());
            var strategy = new RegionSeedingStrategy(_dbContext, _blobStorageService, _loggerFactory.CreateLogger<RegionSeedingStrategy>(), parser, transformer, writer);

            var result = await strategy.SeedAsync(null, forceReseed, cancellationToken);
            return new
            {
                success = result.Success,
                alreadySeeded = result.AlreadySeeded,
                inserted = result.InsertedCount,
                existing = result.ExistingCount,
                errorMessage = result.ErrorMessage
            };
        }
        else
        {
            _logger.LogInformation("Using legacy LocationSeeder for regions");
            await LocationSeeder.SeedRegionsAsync(_dbContext, _blobStorageService, _logger, forceReseed);
            return new { success = true, message = "Legacy seeder completed" };
        }
    }

    private async Task<object> SeedProvincesAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
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
            return new
            {
                success = result.Success,
                alreadySeeded = result.AlreadySeeded,
                inserted = result.InsertedCount,
                existing = result.ExistingCount,
                errorMessage = result.ErrorMessage
            };
        }
        else
        {
            _logger.LogWarning("Legacy LocationSeeder does not support individual Province seeding. Use optimized seeder instead.");
            return new { success = false, message = "Legacy seeder does not support individual province seeding. Set UseOptimizedLocationSeeder=true in configuration." };
        }
    }

    private async Task<object> SeedMunicipalitiesAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
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
            return new
            {
                success = result.Success,
                alreadySeeded = result.AlreadySeeded,
                inserted = result.InsertedCount,
                existing = result.ExistingCount,
                errorMessage = result.ErrorMessage
            };
        }
        else
        {
            _logger.LogWarning("Legacy LocationSeeder does not support individual Municipality seeding. Use optimized seeder instead.");
            return new { success = false, message = "Legacy seeder does not support individual municipality seeding. Set UseOptimizedLocationSeeder=true in configuration." };
        }
    }

    private async Task<object> SeedBarangaysAsync(bool useOptimizedSeeder, bool forceReseed, CancellationToken cancellationToken)
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
            return new
            {
                success = result.Success,
                alreadySeeded = result.AlreadySeeded,
                inserted = result.InsertedCount,
                existing = result.ExistingCount,
                errorMessage = result.ErrorMessage
            };
        }
        else
        {
            _logger.LogWarning("Legacy LocationSeeder does not support individual Barangay seeding. Use optimized seeder instead.");
            return new { success = false, message = "Legacy seeder does not support individual barangay seeding. Set UseOptimizedLocationSeeder=true in configuration." };
        }
    }
}
