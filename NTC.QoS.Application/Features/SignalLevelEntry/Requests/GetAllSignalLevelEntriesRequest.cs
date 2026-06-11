using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Requests;

public class GetAllSignalLevelEntriesRequest(Guid signalLevelId, int pageSize, int pageNumber) : IRequest<PaginatedResponse<SignalLevelEntryDto>>
{
    public Guid SignalLevelId { get; set; } = signalLevelId;
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
}