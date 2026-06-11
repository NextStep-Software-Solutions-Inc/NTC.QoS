using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Requests;

public class CreateSignalLevelEntryRequest(Guid signalLevelId, CreateSignalLevelEntryDto signalLevelEntry) : IRequest<ApiResponse<SignalLevelEntryDto>>
{
    public Guid SignalLevelId { get; set; } = signalLevelId;
    public CreateSignalLevelEntryDto SignalLevelEntry { get; set; } = signalLevelEntry;
}