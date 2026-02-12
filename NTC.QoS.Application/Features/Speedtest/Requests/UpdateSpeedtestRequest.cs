using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class UpdateSpeedtestRequest(Guid id, UpdateSpeedtestDto updateSpeedtestDto) : IRequest<ApiResponse<SpeedtestDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateSpeedtestDto Speedtest { get; set; } = updateSpeedtestDto;
}
