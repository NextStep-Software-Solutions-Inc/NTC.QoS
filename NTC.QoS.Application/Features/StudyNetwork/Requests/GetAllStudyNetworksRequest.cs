using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Requests;

public class GetAllStudyNetworksRequest(int pageSize,
                                        int pageNumber,
                                        List<Guid>? studyIds,
                                        List<Guid>? networkIds) : IRequest<PaginatedResponse<StudyNetworkDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public List<Guid>? StudyIds { get; set; } = studyIds;
    public List<Guid>? NetworkIds { get; set; } = networkIds;
}
