using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Requests;

public class GetAllStudyMunicipalitiesRequest(int pageSize, int pageNumber, Guid? municipalityId, Guid? studyId) : IRequest<PaginatedResponse<StudyMunicipalityDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? StudyId { get; set; } = studyId;
    public Guid? MunicipalityId { get; set; } = municipalityId;
}
