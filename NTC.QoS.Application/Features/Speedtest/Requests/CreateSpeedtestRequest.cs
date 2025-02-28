using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class CreateSpeedtestRequest : IRequest<ApiResponse<CreateSpeedtestDto>>
{
    public CreateSpeedtestRequest(CreateSpeedtestDto speedtest)
    {
        Speedtest = speedtest;
    }
    public CreateSpeedtestDto Speedtest { get; set; }
}