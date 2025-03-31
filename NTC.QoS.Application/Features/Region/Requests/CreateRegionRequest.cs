using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Requests;

public class CreateRegionRequest : IRequest<ApiResponse<RegionDto>>
{
    public required CreateRegionDto Region { get; set; }
}
