using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Requests;

public class UpdateZoneByIdRequest : IRequest<ApiResponse<Domain.Entities.Zone>>
{
    public UpdateZoneByIdRequest(Guid id, UpdateZoneDto zone)
    {
        Id = id;
        Zone = zone;
    }
    public Guid Id { get; set; }
    public UpdateZoneDto Zone { get; set; }
}
