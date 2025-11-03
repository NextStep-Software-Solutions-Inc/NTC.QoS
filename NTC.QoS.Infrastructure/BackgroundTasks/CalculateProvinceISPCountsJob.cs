using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job to pre-calculate and cache ISP counts for all provinces
/// This solves the PostgreSQL shared memory issue by eliminating real-time spatial queries
/// Runs periodically (recommended: every 6-12 hours) to keep data fresh
/// </summary>
public class CalculateProvinceISPCountsJob : ICalculateProvinceISPCountsJob
{
    private readonly ILogger<CalculateProvinceISPCountsJob> _logger;
    private readonly IRepository<Province> _provinceRepository;
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly IRepository<ProvinceISPCountCache> _cacheRepository;
    
    private const int BATCH_SIZE = 10; // Process 10 provinces at a time to avoid memory pressure

    public CalculateProvinceISPCountsJob(
        ILogger<CalculateProvinceISPCountsJob> logger,
        IRepository<Province> provinceRepository,
        ICellRepository cellRepository,
        INAPRepository napRepository,
        IRepository<ProvinceISPCountCache> cacheRepository)
    {
        _logger = logger;
        _provinceRepository = provinceRepository;
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _cacheRepository = cacheRepository;
    }

    /// <summary>
    /// Execute the background job to calculate ISP counts for all provinces
    /// </summary>
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting province ISP count calculation job at {StartTime}", startTime);

        try
        {
            // Get all provinces with their boundaries
            var provinces = await _provinceRepository.ToListAsync(
                _provinceRepository.Query()
                    .Select(p => new { p.Id, p.Name, p.Boundaries }),
                cancellationToken);

            _logger.LogInformation("Found {ProvinceCount} provinces to process", provinces.Count);

            var totalProcessed = 0;
            var totalBatches = (int)Math.Ceiling((double)provinces.Count / BATCH_SIZE);

            // Process provinces in batches to avoid memory issues
            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = provinces
                    .Skip(batchIndex * BATCH_SIZE)
                    .Take(BATCH_SIZE)
                    .ToList();

                _logger.LogInformation("Processing batch {BatchIndex}/{TotalBatches} ({BatchSize} provinces)",
                    batchIndex + 1, totalBatches, batch.Count);

                foreach (var province in batch)
                {
                    try
                    {
                        var ispCount = await CalculateISPCountForProvince(province.Id, province.Boundaries, cancellationToken);

                        // Update or create cache entry
                        var existingCache = await _cacheRepository.FirstOrDefaultAsync(
                            _cacheRepository.Query(c => c.ProvinceId == province.Id),
                            cancellationToken);

                        if (existingCache != null)
                        {
                            existingCache.ISPCount = ispCount;
                            existingCache.LastCalculated = DateTime.UtcNow;
                            _cacheRepository.Update(existingCache);
                        }
                        else
                        {
                            await _cacheRepository.AddAsync(new ProvinceISPCountCache
                            {
                                ProvinceId = province.Id,
                                ISPCount = ispCount,
                                LastCalculated = DateTime.UtcNow
                            }, cancellationToken);
                        }

                        totalProcessed++;

                        _logger.LogDebug("Province {ProvinceName} ({ProvinceId}): {ISPCount} ISPs",
                            province.Name, province.Id, ispCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calculating ISP count for province {ProvinceId}", province.Id);
                        // Continue with next province
                    }
                }

                // Save changes for this batch
                await _cacheRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Batch {BatchIndex}/{TotalBatches} completed. Total processed: {TotalProcessed}/{TotalProvinces}",
                    batchIndex + 1, totalBatches, totalProcessed, provinces.Count);

                // Small delay between batches to reduce database pressure
                if (batchIndex < totalBatches - 1)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            _logger.LogInformation(
                "Province ISP count calculation completed successfully. " +
                "Processed {TotalProcessed}/{TotalProvinces} provinces in {Duration:mm\\:ss}",
                totalProcessed, provinces.Count, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in province ISP count calculation job");
            throw; // Re-throw for Hangfire retry logic
        }
    }

    /// <summary>
    /// Calculate unique ISP count for a single province using its boundary
    /// </summary>
    private async Task<int> CalculateISPCountForProvince(
        Guid provinceId,
        NetTopologySuite.Geometries.MultiPolygon? boundary,
        CancellationToken cancellationToken)
    {
        if (boundary == null)
        {
            _logger.LogWarning("Province {ProvinceId} has no boundary defined, returning 0 ISPs", provinceId);
            return 0;
        }

        try
        {
            // Get unique service providers from cells within province boundary
            var cellProviderIds = await _cellRepository.ToListAsync(
                _cellRepository.Query()
                    .Where(cell => boundary.IsWithinDistance(cell.Location, 0))
                    .Select(cell => cell.ServiceProviderId)
                    .Distinct(),
                cancellationToken);

            // Get unique service providers from NAPs within province boundary  
            var napProviderIds = await _napRepository.ToListAsync(
                _napRepository.Query()
                    .Where(nap => boundary.IsWithinDistance(nap.Location, 0))
                    .Select(nap => nap.ServiceProviderId)
                    .Distinct(),
                cancellationToken);

            // Combine and count unique service providers
            var allProviderIds = new HashSet<Guid>(cellProviderIds);
            allProviderIds.UnionWith(napProviderIds);

            return allProviderIds.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing spatial query for province {ProvinceId}", provinceId);
            return 0; // Return 0 on error to prevent job failure
        }
    }
}
