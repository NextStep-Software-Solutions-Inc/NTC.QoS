using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Requests;

public class CreateMIMORequest : IRequest<ApiResponse<MIMODto>>
{
    public CreateMIMORequest(CreateMIMODto mimo)
    {
        MIMO = mimo;
    }
    public CreateMIMODto MIMO { get; set; }
}
