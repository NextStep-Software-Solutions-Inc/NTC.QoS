using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Requests;

public class UpdateSignalLevelEntryByIdRequest(Guid signalLevelId, Guid id, UpdateSignalLevelEntryDto signalLevelEntry) : IRequest<ApiResponse<SignalLevelEntryDto>>
{
    public Guid SignalLevelId { get; set; } = signalLevelId;
    public Guid Id { get; set; } = id;
    public UpdateSignalLevelEntryDto SignalLevelEntry { get; set; } = signalLevelEntry;
}