using MediatR;
namespace NTC.QoS.Application.Features.Generation.Requests;

public class DeleteGenerationByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteGenerationByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


