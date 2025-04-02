using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Requests;

public class GetStudyMunicipalityByIdRequest(Guid id) : IRequest<ApiResponse<StudyMunicipalityDto>>
{
    public Guid Id { get; set; } = id;
}
