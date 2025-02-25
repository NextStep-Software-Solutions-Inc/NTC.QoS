using MediatR;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class DeleteServiceProviderByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteServiceProviderByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
