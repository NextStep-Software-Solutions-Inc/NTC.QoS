using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Requests;

public class GetRegionByIdRequest : IRequest<ApiResponse<RegionDto>>
{
    public GetRegionByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


