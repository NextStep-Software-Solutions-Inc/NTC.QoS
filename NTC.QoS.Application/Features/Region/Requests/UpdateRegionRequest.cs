using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Requests;

public class UpdateRegionRequest : IRequest<ApiResponse<RegionDto>>
{
    public Guid Id { get; set; }
    public UpdateRegionDto Region { get; set; }
}
