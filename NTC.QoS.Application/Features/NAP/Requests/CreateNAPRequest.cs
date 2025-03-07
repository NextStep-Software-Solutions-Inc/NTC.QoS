using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class CreateNAPRequest : IRequest<ApiResponse<CreateNAPDto>>
{
    public required CreateNAPDto CreateNAP { get; set; }
}
