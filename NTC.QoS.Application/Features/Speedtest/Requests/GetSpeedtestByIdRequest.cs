using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;
public class GetSpeedtestByIdRequest : IRequest<ApiResponse<SpeedtestDto>>
{
    public GetSpeedtestByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


