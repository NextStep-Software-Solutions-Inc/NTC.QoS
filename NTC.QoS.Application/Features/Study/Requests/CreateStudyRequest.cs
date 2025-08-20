using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Requests;

public class CreateStudyRequest(CreateStudyDto study) : IRequest<ApiResponse<StudyDto>>
{
    public CreateStudyDto Study { get; set; } = study;
}
