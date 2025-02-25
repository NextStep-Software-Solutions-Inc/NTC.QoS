using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.AreaStudy.Requests;

public class DeleteAreaStudyByIdRequest : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public DeleteAreaStudyByIdRequest(Guid id)
    {
        Id = id;
    }
}
