using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class CreateSpeedtestRequest(CreateSpeedtestDto speedtest) : IRequest<ApiResponse<Guid>>
{
    public CreateSpeedtestDto Speedtest { get; set; } = speedtest;
}