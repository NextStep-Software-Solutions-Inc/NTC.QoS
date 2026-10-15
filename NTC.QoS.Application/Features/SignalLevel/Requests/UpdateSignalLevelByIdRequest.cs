using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Requests;

public class UpdateSignalLevelByIdRequest(Guid id, UpdateSignalLevelDto signalLevel) : IRequest<ApiResponse<SignalLevelDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateSignalLevelDto SignalLevel { get; set; } = signalLevel;
}