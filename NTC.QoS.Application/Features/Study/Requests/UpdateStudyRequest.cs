using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Requests;

public class UpdateStudyRequest(Guid id, UpdateStudyDto study) : IRequest<ApiResponse<StudyDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateStudyDto Study { get; set; } = study;
}
