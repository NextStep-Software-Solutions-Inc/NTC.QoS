using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed.Interfaces;
using NTC.QoS.Infrastructure.Seed.Models;

namespace NTC.QoS.Infrastructure.Seed.Strategies;

/// <summary>
/// Base strategy for seeding location entities with SOLID principles.
/// </summary>
/// <typeparam name="TEntity">The entity type to seed.</typeparam>
public abstract class LocationSeedingStrategy<TEntity> where TEntity : class
{
    protected readonly ApplicationDbContext DbContext;
    protected readonly IBlobStorageService BlobStorageService;
    protected readonly ILogger Logger;
    protected readonly ILocationDataParser<ParsedLocationFeature> Parser;
    protected readonly ILocationDataTransformer<ParsedLocationFeature, TEntity> Transformer;
    protected readonly ILocationDataWriter<TEntity> Writer;

    protected LocationSeedingStrategy(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ILogger logger,
        ILocationDataParser<ParsedLocationFeature> parser,
        ILocationDataTransformer<ParsedLocationFeature, TEntity> transformer,
        ILocationDataWriter<TEntity> writer)
    {
        DbContext = dbContext;
        BlobStorageService = blobStorageService;
        Logger = logger;
        Parser = parser;
        Transformer = transformer;
        Writer = writer;
    }

    protected abstract string ContainerName { get; }
    protected abstract string FilePath { get; }
    protected abstract string EntityName { get; }
    protected abstract WriteOptions GetWriteOptions();

    public async Task<SeedingResult> SeedAsync(
        Dictionary<string, Guid>? parentIdMap = null,
        bool forceReseed = false,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting {EntityName} seeding from {Container}/{Path}",
            EntityName, ContainerName, FilePath);

        // Check if data already exists
        if (!forceReseed)
        {
            var existingCount = await GetExistingCountAsync(cancellationToken);
            if (existingCount > 0)
            {
                Logger.LogInformation("{EntityName} already exist ({Count} found). Skipping seeding.",
                    EntityName, existingCount);
                return new SeedingResult { AlreadySeeded = true, ExistingCount = existingCount };
            }
        }

        // Check if file exists
        if (!await BlobStorageService.ExistsAsync(ContainerName, FilePath, cancellationToken))
        {
            Logger.LogError("GeoJSON file not found: {Container}/{Path}", ContainerName, FilePath);
            return new SeedingResult { Success = false, ErrorMessage = "File not found" };
        }

        try
        {
            // Download and parse stream
            await using var stream = await BlobStorageService.DownloadAsync(ContainerName, FilePath, cancellationToken);

            var parsedFeatures = Parser.ParseFeaturesAsync(stream, cancellationToken);

            // Transform to entities
            var context = new TransformContext
            {
                ParentIdMap = parentIdMap ?? new Dictionary<string, Guid>(),
                Timestamp = DateTime.UtcNow,
                UserId = "System"
            };

            var transformedEntities = TransformFeaturesAsync(parsedFeatures, context, cancellationToken);

            // Filter out existing entities only if not force reseeding
            IAsyncEnumerable<TEntity> entitiesToWrite;
            if (forceReseed)
            {
                Logger.LogInformation("Force reseed enabled - all entities will be updated/inserted");
                entitiesToWrite = transformedEntities;
            }
            else
            {
                var existingCodes = await GetExistingCodesAsync(cancellationToken);
                entitiesToWrite = FilterNewEntitiesAsync(transformedEntities, existingCodes, cancellationToken);
            }

            // Write to database
            var options = GetWriteOptions();
            options = options with
            {
                ForceReseed = forceReseed,
                ProgressCallback = count => Logger.LogInformation("Seeded {Count} {EntityName}...", count, EntityName)
            };

            var written = await Writer.WriteBatchAsync(entitiesToWrite, options, cancellationToken);

            Logger.LogInformation("Completed {EntityName} seeding. Inserted: {Count}", EntityName, written);

            return new SeedingResult
            {
                Success = true,
                InsertedCount = written
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seed {EntityName}", EntityName);
            return new SeedingResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    protected abstract Task<int> GetExistingCountAsync(CancellationToken cancellationToken);
    protected abstract Task<HashSet<string>> GetExistingCodesAsync(CancellationToken cancellationToken);

    private async IAsyncEnumerable<TEntity> TransformFeaturesAsync(
        IAsyncEnumerable<ParsedLocationFeature> features,
        TransformContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var feature in features.WithCancellation(cancellationToken))
        {
            var entity = Transformer.Transform(feature, context);
            if (entity != null)
            {
                yield return entity;
            }
        }
    }

    private async IAsyncEnumerable<TEntity> FilterNewEntitiesAsync(
        IAsyncEnumerable<TEntity> entities,
        HashSet<string> existingCodes,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var entity in entities.WithCancellation(cancellationToken))
        {
            var code = GetEntityCode(entity);
            if (!string.IsNullOrEmpty(code) && !existingCodes.Contains(code))
            {
                yield return entity;
            }
        }
    }

    protected abstract string? GetEntityCode(TEntity entity);
}

/// <summary>
/// Result of a seeding operation.
/// </summary>
public class SeedingResult
{
    public bool Success { get; init; }
    public bool AlreadySeeded { get; init; }
    public int ExistingCount { get; init; }
    public int InsertedCount { get; init; }
    public string? ErrorMessage { get; init; }
}
