using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class CreateNAPRequest : IRequest<ApiResponse<NAPDto>>
{
    public CreateNAPDto NAP { get; set; }
    public CreateNAPRequest(CreateNAPDto createNAPDto)
    {
        NAP = createNAPDto;
    }
}
