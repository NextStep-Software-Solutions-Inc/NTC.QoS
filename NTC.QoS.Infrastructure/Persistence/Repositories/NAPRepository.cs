using System.Runtime.CompilerServices; 
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NAPRepository(ApplicationDbContext context) : Repository<NAP>(context), INAPRepository
{
    public async Task<List<ClusteredNapResultDto>> GetClusteredNapsAsync(
        double clusteringDistance,
        int minPoints = 2 // New parameter for minpoints, default to 2
    )
    {
        // Use FormattableStringFactory.Create for safe parameterization with FromSqlInterpolated
        // This is generally preferred over FromSqlRaw when you have parameters.
        FormattableString sql = FormattableStringFactory.Create($@"
            WITH ClusteredNAPs AS (
                SELECT
                    nap.""Id"",
                    nap.""Location"",
                    nap.""ServiceProviderId"",
                    ST_Transform(nap.""Location""::geometry, 32651) AS transformed_location,
                    ST_ClusterDBSCAN(
                        ST_Transform(nap.""Location""::geometry, 32651),
                        eps := {clusteringDistance},
                        minpoints := {minPoints}
                    ) OVER() AS ClusterId
                FROM
                    public.""NAPs"" nap
            ),
            ClusterCentroids AS (
                SELECT
                    ClusterId,
                    COUNT(*) AS PointCount,
                    ST_Centroid(ST_Collect(transformed_location)) AS CentroidGeom
                FROM
                    ClusteredNAPs
                WHERE
                    ClusterId IS NOT NULL
                GROUP BY
                    ClusterId
            )
            SELECT DISTINCT ON (cn.ClusterId)
                cn.ClusterId,
                cc.PointCount,
                cn.""Id"" AS Id,
                cn.""ServiceProviderId"" AS ServiceProviderId,
                ST_Y(cn.""Location""::geometry) AS Latitude,
                ST_X(cn.""Location""::geometry) AS Longitude,
                ST_Distance(cn.transformed_location, cc.CentroidGeom) AS Distance
            FROM
                ClusteredNAPs cn
            JOIN
                ClusterCentroids cc ON cn.ClusterId = cc.ClusterId
            WHERE
                cn.ClusterId IS NOT NULL
            ORDER BY
                cn.ClusterId,
                ST_Distance(cn.transformed_location, cc.CentroidGeom) ASC;
        ");
        return await context.ClusteredNapResults
                            .FromSqlInterpolated(sql) // Use FromSqlInterpolated for safety
                            .ToListAsync();
    }
}