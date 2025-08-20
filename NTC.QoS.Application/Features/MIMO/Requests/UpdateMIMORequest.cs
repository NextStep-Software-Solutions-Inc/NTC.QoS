using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Requests;

public class UpdateMIMOByIdRequest : IRequest<ApiResponse<MIMODto>>
{
    public Guid Id { get; set; }
    public UpdateMIMODto MIMO { get; set; } = null!;
    public UpdateMIMOByIdRequest(Guid id, UpdateMIMODto mimo)
    {
        Id = id;
        MIMO = mimo;
    }
}
