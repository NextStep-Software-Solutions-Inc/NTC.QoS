using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Requests;

public class CreateSignalLevelRequest(CreateSignalLevelDto signalLevel) : IRequest<ApiResponse<SignalLevelDto>>
{
    public CreateSignalLevelDto SignalLevel { get; set; } = signalLevel;
}