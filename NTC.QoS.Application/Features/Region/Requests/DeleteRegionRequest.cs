using MediatR;

namespace NTC.QoS.Application.Features.Region.Requests;

public class DeleteRegionRequest : IRequest<ApiResponse<bool>>
{
    public DeleteRegionRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
