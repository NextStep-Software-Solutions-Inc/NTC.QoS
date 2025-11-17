using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed.Interfaces;
using System.Diagnostics;
using Npgsql;
using NetTopologySuite.IO;

namespace NTC.QoS.Infrastructure.Seed.Writers;

/// <summary>
/// Writes location entities in optimized batches with configurable options.
/// </summary>
/// <typeparam name="TEntity">The entity type to write.</typeparam>
public class BatchedLocationWriter<TEntity> : ILocationDataWriter<TEntity> where TEntity : class
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BatchedLocationWriter<TEntity>> _logger;

    public BatchedLocationWriter(
        ApplicationDbContext dbContext,
        ILogger<BatchedLocationWriter<TEntity>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> WriteBatchAsync(
        IAsyncEnumerable<TEntity> entities,
        WriteOptions options,
        CancellationToken cancellationToken = default)
    {
        var totalWritten = 0;
        var batch = new List<TEntity>(options.BatchSize);

        await foreach (var entity in entities.WithCancellation(cancellationToken))
        {
            batch.Add(entity);

            if (batch.Count >= options.BatchSize)
            {
                var written = await WriteSingleBatchAsync(batch, options, cancellationToken);
                totalWritten += written;

                options.ProgressCallback?.Invoke(totalWritten);

                batch.Clear();
            }
        }

        // Write remaining entities
        if (batch.Count > 0)
        {
            var written = await WriteSingleBatchAsync(batch, options, cancellationToken);
            totalWritten += written;
            options.ProgressCallback?.Invoke(totalWritten);
        }

        _logger.LogInformation("Completed writing {Count} {EntityType} entities",
            totalWritten, typeof(TEntity).Name);

        return totalWritten;
    }

    private async Task<int> WriteSingleBatchAsync(
        List<TEntity> batch,
        WriteOptions options,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return 0;

        try
        {
            // Set command timeout specifically for seeding operations to handle large geometry data
            var originalTimeout = _dbContext.Database.GetCommandTimeout();
            _dbContext.Database.SetCommandTimeout(options.TimeoutSeconds);

            try
            {
                _logger.LogDebug("Starting to save {Count} {EntityType} entities", batch.Count, typeof(TEntity).Name);
                var stopwatch = Stopwatch.StartNew();

                // Use raw SQL insert to avoid EF geometry copying and OOM
                foreach (var entity in batch)
                {
                    var wkb = GetGeometryWkb(entity);
                    var sql = GetInsertSql(typeof(TEntity).Name);
                    var parameters = GetParameters(entity, typeof(TEntity).Name);
                    await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
                }

                stopwatch.Stop();
                _logger.LogDebug("Successfully saved {Count} {EntityType} entities in {Time}ms", batch.Count, typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);

                return batch.Count;
            }
            finally
            {
                // Restore original timeout
                _dbContext.Database.SetCommandTimeout(originalTimeout);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write batch of {Count} {EntityType} entities",
                batch.Count, typeof(TEntity).Name);
            throw;
        }
    }

    private static byte[] GetGeometryWkb(object entity)
    {
        var boundaries = entity.GetType().GetProperty("Boundaries")?.GetValue(entity) as NetTopologySuite.Geometries.Geometry;
        return boundaries?.ToBinary() ?? Array.Empty<byte>();
    }

    private static byte[] GetCenterWkb(object entity)
    {
        var center = entity.GetType().GetProperty("Center")?.GetValue(entity) as NetTopologySuite.Geometries.Point;
        return center?.ToBinary() ?? Array.Empty<byte>();
    }

    private static Guid? GetRegionId(object entity) => entity.GetType().GetProperty("RegionId")?.GetValue(entity) as Guid?;
    private static Guid? GetProvinceId(object entity) => entity.GetType().GetProperty("ProvinceId")?.GetValue(entity) as Guid?;
    private static Guid? GetMunicipalityId(object entity) => entity.GetType().GetProperty("MunicipalityId")?.GetValue(entity) as Guid?;
    private static bool? GetIsCity(object entity) => entity.GetType().GetProperty("IsCity")?.GetValue(entity) as bool?;

    private static string GetInsertSql(string entityType) => entityType switch
    {
        "Region" => "INSERT INTO \"Regions\" (\"Id\", \"Code\", \"Name\", \"Description\", \"Area\", \"TotalPopulation\", \"HouseholdPopulation\", \"HouseholdCount\", \"Boundaries\", \"Center\", \"IsDeleted\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\") VALUES (@id, @code, @name, @description, @area, @totalPopulation, @householdPopulation, @householdCount, ST_GeomFromWKB(@boundaries, 4326), ST_GeomFromWKB(@center, 4326), @isDeleted, @createdAt, @createdBy, @updatedAt, @updatedBy)",
        "Province" => "INSERT INTO \"Provinces\" (\"Id\", \"Code\", \"Name\", \"Description\", \"Area\", \"TotalPopulation\", \"HouseholdPopulation\", \"HouseholdCount\", \"Boundaries\", \"Center\", \"RegionId\", \"IsDeleted\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\") VALUES (@id, @code, @name, @description, @area, @totalPopulation, @householdPopulation, @householdCount, ST_GeomFromWKB(@boundaries, 4326), ST_GeomFromWKB(@center, 4326), @regionId, @isDeleted, @createdAt, @createdBy, @updatedAt, @updatedBy)",
        "Municipality" => "INSERT INTO \"Municipalities\" (\"Id\", \"Code\", \"Name\", \"Description\", \"Area\", \"TotalPopulation\", \"HouseholdPopulation\", \"HouseholdCount\", \"Boundaries\", \"Center\", \"ProvinceId\", \"IsCity\", \"IsDeleted\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\") VALUES (@id, @code, @name, @description, @area, @totalPopulation, @householdPopulation, @householdCount, ST_GeomFromWKB(@boundaries, 4326), ST_GeomFromWKB(@center, 4326), @provinceId, @isCity, @isDeleted, @createdAt, @createdBy, @updatedAt, @updatedBy)",
        "Barangay" => "INSERT INTO \"Barangays\" (\"Id\", \"Code\", \"Name\", \"Description\", \"Area\", \"TotalPopulation\", \"HouseholdPopulation\", \"HouseholdCount\", \"Boundaries\", \"Center\", \"MunicipalityId\", \"IsDeleted\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\") VALUES (@id, @code, @name, @description, @area, @totalPopulation, @householdPopulation, @householdCount, ST_GeomFromWKB(@boundaries, 4326), ST_GeomFromWKB(@center, 4326), @municipalityId, @isDeleted, @createdAt, @createdBy, @updatedAt, @updatedBy)",
        _ => throw new NotSupportedException($"Entity type {entityType} not supported")
    };

    private static NpgsqlParameter[] GetParameters(object entity, string entityType)
    {
        var parameters = new List<NpgsqlParameter>
        {
            new NpgsqlParameter("@id", entity.GetType().GetProperty("Id")?.GetValue(entity)),
            new NpgsqlParameter("@code", entity.GetType().GetProperty("Code")?.GetValue(entity)),
            new NpgsqlParameter("@name", entity.GetType().GetProperty("Name")?.GetValue(entity)),
            new NpgsqlParameter("@description", entity.GetType().GetProperty("Description")?.GetValue(entity) ?? DBNull.Value),
            new NpgsqlParameter("@area", entity.GetType().GetProperty("Area")?.GetValue(entity) ?? 0f),
            new NpgsqlParameter("@totalPopulation", entity.GetType().GetProperty("TotalPopulation")?.GetValue(entity) ?? DBNull.Value),
            new NpgsqlParameter("@householdPopulation", entity.GetType().GetProperty("HouseholdPopulation")?.GetValue(entity) ?? DBNull.Value),
            new NpgsqlParameter("@householdCount", entity.GetType().GetProperty("HouseholdCount")?.GetValue(entity) ?? DBNull.Value),
            new NpgsqlParameter("@boundaries", GetGeometryWkb(entity)),
            new NpgsqlParameter("@center", GetCenterWkb(entity)),
            new NpgsqlParameter("@isDeleted", entity.GetType().GetProperty("IsDeleted")?.GetValue(entity)),
            new NpgsqlParameter("@createdAt", entity.GetType().GetProperty("CreatedAt")?.GetValue(entity)),
            new NpgsqlParameter("@createdBy", entity.GetType().GetProperty("CreatedBy")?.GetValue(entity)),
            new NpgsqlParameter("@updatedAt", entity.GetType().GetProperty("UpdatedAt")?.GetValue(entity)),
            new NpgsqlParameter("@updatedBy", entity.GetType().GetProperty("UpdatedBy")?.GetValue(entity))
        };

        switch (entityType)
        {
            case "Province":
                parameters.Add(new NpgsqlParameter("@regionId", GetRegionId(entity)));
                break;
            case "Municipality":
                parameters.Add(new NpgsqlParameter("@provinceId", GetProvinceId(entity)));
                parameters.Add(new NpgsqlParameter("@isCity", GetIsCity(entity)));
                break;
            case "Barangay":
                parameters.Add(new NpgsqlParameter("@municipalityId", GetMunicipalityId(entity)));
                break;
        }

        return parameters.ToArray();
    }
}
