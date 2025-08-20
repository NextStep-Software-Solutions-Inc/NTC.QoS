using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Requests;

public class CreateAreaStudyRequest(CreateAreaStudyDto areaStudy) : IRequest<ApiResponse<AreaStudyDto>>
{
    public CreateAreaStudyDto AreaStudy { get; set; } = areaStudy;
}