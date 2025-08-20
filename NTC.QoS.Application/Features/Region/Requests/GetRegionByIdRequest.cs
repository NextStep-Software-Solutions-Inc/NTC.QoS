using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;
namespace NTC.QoS.Application.Features.Region.Requests;

public class GetRegionByIdRequest(Guid id) : IRequest<ApiResponse<RegionDto>>
{
    public Guid Id { get; set; } = id;
}


