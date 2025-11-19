using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.HealthChecks;

/// <summary>
/// Health check that reports Degraded if there are pending EF Core migrations.
/// Useful for readiness probes so traffic isn't fully routed until schema matches.
/// </summary>
public class PendingMigrationsHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    public PendingMigrationsHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            return HealthCheckResult.Degraded($"Pending migrations: {string.Join(", ", pending)}");
        }
        return HealthCheckResult.Healthy("Database schema is up to date.");
    }
}
