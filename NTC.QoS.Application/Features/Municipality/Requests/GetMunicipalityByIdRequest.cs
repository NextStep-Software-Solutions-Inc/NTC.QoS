using MediatR;
using NTC.QoS.Domain.Models.DTOs.Municipality;
namespace NTC.QoS.Application.Features.Municipality.Requests;

public class GetMunicipalityByIdRequest : IRequest<ApiResponse<MunicipalityDto>>
{
    public GetMunicipalityByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


