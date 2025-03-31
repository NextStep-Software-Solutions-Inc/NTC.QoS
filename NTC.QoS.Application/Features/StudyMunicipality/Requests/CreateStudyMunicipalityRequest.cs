using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Requests;

public class CreateStudyMunicipalityRequest(CreateStudyMunicipalityDto studyMunicipality) : IRequest<ApiResponse<StudyMunicipalityDto>>
{
    public CreateStudyMunicipalityDto StudyMunicipality { get; set; } = studyMunicipality;
}