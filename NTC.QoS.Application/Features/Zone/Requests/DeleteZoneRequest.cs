using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Zone.Requests;

public class DeleteZoneByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteZoneByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


