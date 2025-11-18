using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed.Interfaces;
using NTC.QoS.Infrastructure.Seed.Models;

namespace NTC.QoS.Infrastructure.Seed.Strategies;

/// <summary>
/// Seeding strategy for Region entities.
/// </summary>
public class RegionSeedingStrategy : LocationSeedingStrategy<Region>
{
    public RegionSeedingStrategy(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ILogger<RegionSeedingStrategy> logger,
        ILocationDataParser<ParsedLocationFeature> parser,
        ILocationDataTransformer<ParsedLocationFeature, Region> transformer,
        ILocationDataWriter<Region> writer)
        : base(dbContext, blobStorageService, logger, parser, transformer, writer)
    {
    }

    protected override string ContainerName => "seeds";
    protected override string FilePath => "boundaries/Regionbdry.geojson";
    protected override string EntityName => "Regions";

    protected override WriteOptions GetWriteOptions() => new()
    {
        BatchSize = 1,  // Reduced to 1 for very large geometries (200MB+ per feature)
        TimeoutSeconds = 150,
        EnableStreaming = true
    };

    protected override async Task<int> GetExistingCountAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Regions.CountAsync(cancellationToken);
    }

    protected override async Task<HashSet<string>> GetExistingCodesAsync(CancellationToken cancellationToken)
    {
        var codes = await DbContext.Regions
            .AsNoTracking()
            .Select(r => r.Code)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }

    protected override string? GetEntityCode(Region entity) => entity.Code;
}

/// <summary>
/// Seeding strategy for Province entities.
/// </summary>
public class ProvinceSeedingStrategy : LocationSeedingStrategy<Province>
{
    public ProvinceSeedingStrategy(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ILogger<ProvinceSeedingStrategy> logger,
        ILocationDataParser<ParsedLocationFeature> parser,
        ILocationDataTransformer<ParsedLocationFeature, Province> transformer,
        ILocationDataWriter<Province> writer)
        : base(dbContext, blobStorageService, logger, parser, transformer, writer)
    {
    }

    protected override string ContainerName => "seeds";
    protected override string FilePath => "boundaries/Provincebdry.geojson";
    protected override string EntityName => "Provinces";

    protected override WriteOptions GetWriteOptions() => new()
    {
        BatchSize = 5,  // Very small batches for geometry writes
        TimeoutSeconds = 300,
        EnableStreaming = true
    };

    protected override async Task<int> GetExistingCountAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Provinces.CountAsync(cancellationToken);
    }

    protected override async Task<HashSet<string>> GetExistingCodesAsync(CancellationToken cancellationToken)
    {
        var codes = await DbContext.Provinces
            .AsNoTracking()
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }

    protected override string? GetEntityCode(Province entity) => entity.Code;
}

/// <summary>
/// Seeding strategy for Municipality entities.
/// </summary>
public class MunicipalitySeedingStrategy : LocationSeedingStrategy<Municipality>
{
    public MunicipalitySeedingStrategy(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ILogger<MunicipalitySeedingStrategy> logger,
        ILocationDataParser<ParsedLocationFeature> parser,
        ILocationDataTransformer<ParsedLocationFeature, Municipality> transformer,
        ILocationDataWriter<Municipality> writer)
        : base(dbContext, blobStorageService, logger, parser, transformer, writer)
    {
    }

    protected override string ContainerName => "seeds";
    protected override string FilePath => "boundaries/Municipalitybdry.geojson";
    protected override string EntityName => "Municipalities";

    protected override WriteOptions GetWriteOptions() => new()
    {
        BatchSize = 10,  // Smaller batches for geometry-heavy data
        TimeoutSeconds = 400,
        EnableStreaming = true
    };

    protected override async Task<int> GetExistingCountAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Municipalities.CountAsync(cancellationToken);
    }

    protected override async Task<HashSet<string>> GetExistingCodesAsync(CancellationToken cancellationToken)
    {
        var codes = await DbContext.Municipalities
            .AsNoTracking()
            .Select(m => m.Code)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }

    protected override string? GetEntityCode(Municipality entity) => entity.Code;
}

/// <summary>
/// Seeding strategy for Barangay entities.
/// </summary>
public class BarangaySeedingStrategy : LocationSeedingStrategy<Barangay>
{
    public BarangaySeedingStrategy(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ILogger<BarangaySeedingStrategy> logger,
        ILocationDataParser<ParsedLocationFeature> parser,
        ILocationDataTransformer<ParsedLocationFeature, Barangay> transformer,
        ILocationDataWriter<Barangay> writer)
        : base(dbContext, blobStorageService, logger, parser, transformer, writer)
    {
    }

    protected override string ContainerName => "seeds";
    protected override string FilePath => "boundaries/Barangaybdry.geojson";
    protected override string EntityName => "Barangays";

    protected override WriteOptions GetWriteOptions() => new()
    {
        BatchSize = 5,  // Very small batches for 42k+ records with complex geometries
        TimeoutSeconds = 900,
        EnableStreaming = true
    };

    protected override async Task<int> GetExistingCountAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Barangays.CountAsync(cancellationToken);
    }

    protected override async Task<HashSet<string>> GetExistingCodesAsync(CancellationToken cancellationToken)
    {
        var codes = await DbContext.Barangays
            .AsNoTracking()
            .Select(b => b.Code)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }

    protected override string? GetEntityCode(Barangay entity) => entity.Code;
}
