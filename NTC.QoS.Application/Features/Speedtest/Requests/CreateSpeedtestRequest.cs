using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class CreateSpeedtestRequest(CreateSpeedtestDto speedtest) : IRequest<ApiResponse<Guid>>
{
    public CreateSpeedtestDto Speedtest { get; set; } = speedtest;
    
    /// <summary>
    /// Optional region ID for assessment triggering. If not provided, will attempt spatial lookup.
    /// </summary>
    public Guid? RegionId { get; set; }
}