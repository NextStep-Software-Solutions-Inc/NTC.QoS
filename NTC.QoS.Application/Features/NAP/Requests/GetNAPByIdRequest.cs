using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
namespace NTC.QoS.Application.Features.NAP.Requests;

public class GetNAPByIdRequest : IRequest<ApiResponse<NAPDto>>
{
    public GetNAPByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


