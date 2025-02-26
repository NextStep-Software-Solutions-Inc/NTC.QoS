using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Requests;

public class GetStudyByIdRequest : IRequest<ApiResponse<StudyDto>>
{
    public GetStudyByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
