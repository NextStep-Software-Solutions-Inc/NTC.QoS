using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class DeleteNAPsByIdsRequest : IRequest<ApiResponse<bool>>
{
    public required List<Guid> Ids { get; set; }
    
    /// <summary>
    /// Optional region ID for assessment triggering. If not provided, will attempt spatial lookup.
    /// </summary>
    public Guid? RegionId { get; set; }
}