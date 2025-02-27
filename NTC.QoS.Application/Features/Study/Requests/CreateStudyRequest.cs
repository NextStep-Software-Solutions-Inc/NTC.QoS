using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Requests;

public class CreateStudyRequest : IRequest<ApiResponse<StudyDto>>
{
    public CreateStudyDto Study { get; set; }
    public CreateStudyRequest(CreateStudyDto study)
    {
        Study = study;
    }
}