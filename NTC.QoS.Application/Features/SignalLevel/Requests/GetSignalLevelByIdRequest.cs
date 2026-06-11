using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Requests;

public class GetSignalLevelByIdRequest(Guid id) : IRequest<ApiResponse<SignalLevelDto>>
{
    public Guid Id { get; set; } = id;
}