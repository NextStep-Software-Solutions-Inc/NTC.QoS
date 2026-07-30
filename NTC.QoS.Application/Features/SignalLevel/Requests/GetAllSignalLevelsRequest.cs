using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Requests;

public class GetAllSignalLevelsRequest(int pageSize, int pageNumber, Guid? areaStudyId) : IRequest<PaginatedResponse<SignalLevelDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? AreaStudyId { get; set; } = areaStudyId;
}