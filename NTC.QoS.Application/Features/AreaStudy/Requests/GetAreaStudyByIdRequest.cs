using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Requests;

public class GetAreaStudyByIdRequest(Guid id) : IRequest<ApiResponse<AreaStudyDto>>
{
    public Guid Id { get; set; } = id;
}
