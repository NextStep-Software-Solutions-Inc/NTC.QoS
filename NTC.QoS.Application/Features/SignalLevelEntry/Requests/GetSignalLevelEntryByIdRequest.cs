using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Requests;

public class GetSignalLevelEntryByIdRequest(Guid signalLevelId, Guid id) : IRequest<ApiResponse<SignalLevelEntryDto>>
{
    public Guid SignalLevelId { get; set; } = signalLevelId;
    public Guid Id { get; set; } = id;
}