using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Study.Requests;

public class DeleteStudyByIdRequest : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public DeleteStudyByIdRequest(Guid studyId)
    {
        Id = studyId;
    }
}