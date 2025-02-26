using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Band.Requests;

public class DeleteBandByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteBandByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


