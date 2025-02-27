using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Requests;

public class UpdateAreaStudyByIdRequest : IRequest<ApiResponse<Domain.Entities.AreaStudy>>
{
    public Guid Id { get; set; }
    public AreaStudyDto UpdateAreaStudyDto { get; set; }
    public UpdateAreaStudyByIdRequest(Guid id, AreaStudyDto updateAreaStudyDto)
    {
        Id = id;
        UpdateAreaStudyDto = updateAreaStudyDto;
    }
}