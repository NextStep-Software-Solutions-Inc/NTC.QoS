using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Requests;

public class DeleteSignalLevelEntryByIdRequest(Guid signalLevelId, Guid id) : IRequest<ApiResponse<bool>>
{
    public Guid SignalLevelId { get; set; } = signalLevelId;
    public Guid Id { get; set; } = id;
}