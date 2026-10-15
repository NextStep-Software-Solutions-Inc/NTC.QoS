using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

/// <summary>
/// Returns a signal (row count + last-modified timestamp) that changes whenever the
/// Speedtests table changes, no matter what caused the change. Used by
/// SpeedtestsController to derive cache keys that self-invalidate on any write.
/// </summary>
public class GetSpeedtestsCacheVersionRequest : IRequest<ApiResponse<SpeedtestsCacheVersionDto>>
{
}
