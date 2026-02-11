using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class UpdateNAPRequest(Guid id, UpdateNAPDto updateNAPDto) : IRequest<ApiResponse<NAPDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateNAPDto NAP { get; set; } = updateNAPDto;
}
