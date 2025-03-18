using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Requests;

public class GetMIMOByIdRequest : IRequest<ApiResponse<MIMODto>>
{
    public GetMIMOByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
