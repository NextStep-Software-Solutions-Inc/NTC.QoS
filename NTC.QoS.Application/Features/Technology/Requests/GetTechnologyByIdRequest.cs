using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;
namespace NTC.QoS.Application.Features.Technology.Requests;

public class GetTechnologyByIdRequest : IRequest<ApiResponse<TechnologyDto>>
{
    public GetTechnologyByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


