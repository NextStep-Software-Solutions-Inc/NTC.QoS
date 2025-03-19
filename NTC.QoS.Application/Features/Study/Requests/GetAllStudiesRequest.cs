using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Requests;

public class GetAllStudiesRequest(int pageSize, int pageNumber, Guid? areaStudyId) : IRequest<PaginatedResponse<StudyDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? AreaStudyId { get; set; } = areaStudyId;
}
