using MediatR;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Requests;

public class CreateAreaStudyRequest : IRequest<ApiResponse<Domain.Entities.AreaStudy>>
{
    public required AreaStudyDto AreaStudy { get; set; }
}