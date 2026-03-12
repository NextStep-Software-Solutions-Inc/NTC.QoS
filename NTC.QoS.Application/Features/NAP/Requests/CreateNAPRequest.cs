using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class CreateNAPRequest(CreateNAPDto createNAPDto) : IRequest<ApiResponse<NAPDto>>
{
    public CreateNAPDto NAP { get; set; } = createNAPDto;
    
    /// <summary>
    /// Optional region ID for assessment triggering. If not provided, will attempt spatial lookup.
    /// </summary>
    public Guid? RegionId { get; set; }
}
