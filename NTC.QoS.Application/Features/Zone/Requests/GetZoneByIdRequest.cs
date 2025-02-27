using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;
namespace NTC.QoS.Application.Features.Zone.Requests;

public class GetZoneByIdRequest : IRequest<ApiResponse<ZoneDto>>
{
    public GetZoneByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


