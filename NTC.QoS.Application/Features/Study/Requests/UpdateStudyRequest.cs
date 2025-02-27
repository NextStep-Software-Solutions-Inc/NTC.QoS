using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Requests;

public class UpdateStudyRequest : IRequest<ApiResponse<StudyDto>>
{
    public UpdateStudyRequest(Guid id, UpdateStudyDto study)
    {
        Id = id;
        Study = study;
    }
    public Guid Id { get; set; }
    public UpdateStudyDto Study { get; set; }
}
