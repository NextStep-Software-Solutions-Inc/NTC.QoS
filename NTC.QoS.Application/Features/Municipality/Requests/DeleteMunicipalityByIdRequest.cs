using MediatR;
namespace NTC.QoS.Application.Features.Municipality.Requests;

public class DeleteMunicipalityByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteMunicipalityByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


