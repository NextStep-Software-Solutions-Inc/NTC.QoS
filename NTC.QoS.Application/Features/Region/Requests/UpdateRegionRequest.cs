using MediatR;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Requests;

public class UpdateRegionRequest : IRequest<ApiResponse<UpdateRegionDto>>
{
    public Guid Id { get; set; }
    public UpdateRegionDto Region { get; set; } = null!;
}
