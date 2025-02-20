using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Requests;

public class CreateRegionRequest : IRequest<ApiResponse<CreateRegionDto>>
{
    public CreateRegionDto Region { get; set; } = new();
}
