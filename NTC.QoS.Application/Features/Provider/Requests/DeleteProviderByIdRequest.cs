using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Provider.Requests;

public class DeleteProviderByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteProviderByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


