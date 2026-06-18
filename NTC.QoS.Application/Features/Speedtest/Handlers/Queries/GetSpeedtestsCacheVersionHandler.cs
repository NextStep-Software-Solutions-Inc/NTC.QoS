using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

// NOTE: namespace assumed to parallel CreateSpeedtestHandler's
// "NTC.QoS.Application.Features.Speedtest.Handlers.Commands" — adjust if this
// codebase's query handlers actually live somewhere else (e.g. ".Handlers.Queries"
// vs ".Handlers.Query", or no Commands/Queries split at all).
namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

/// <summary>
/// Computes a version signal for the Speedtests table: total row count and the most
/// recent CreatedAt/UpdatedAt across all rows. Both are pulled in a single grouped
/// aggregate query (one round trip) rather than two separate aggregate calls, since
/// running two queries concurrently against the same ApplicationDbContext instance
/// isn't safe.
/// </summary>
public class GetSpeedtestsCacheVersionHandler(ISpeedtestRepository speedtestRepository)
    : IRequestHandler<GetSpeedtestsCacheVersionRequest, ApiResponse<SpeedtestsCacheVersionDto>>
{
    private sealed record VersionAggregate(int Count, DateTime MaxCreatedAt, DateTime MaxUpdatedAt);

    public async Task<ApiResponse<SpeedtestsCacheVersionDto>> Handle(GetSpeedtestsCacheVersionRequest request, CancellationToken cancellationToken)
    {
        // QueryAsNoTracking() (not the overridden Query()) is used deliberately here —
        // it skips SpeedtestRepository's ServiceProvider/Generation/Technology includes,
        // which this aggregate-only query has no use for.
        var aggregateQuery = speedtestRepository.QueryAsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new VersionAggregate(
                g.Count(),
                g.Max(s => s.CreatedAt),
                g.Max(s => s.UpdatedAt)));

        var aggregate = await speedtestRepository.FirstOrDefaultAsync(aggregateQuery, cancellationToken);

        // aggregate is null only when the table is empty (GroupBy produces no groups).
        var lastModifiedAt = aggregate is null
            ? DateTime.MinValue
            : aggregate.MaxUpdatedAt > aggregate.MaxCreatedAt ? aggregate.MaxUpdatedAt : aggregate.MaxCreatedAt;

        return new ApiResponse<SpeedtestsCacheVersionDto>
        {
            Data = new SpeedtestsCacheVersionDto(aggregate?.Count ?? 0, lastModifiedAt),
            Succeeded = true
        };
    }
}
