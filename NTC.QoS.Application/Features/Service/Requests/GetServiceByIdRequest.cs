using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class GetServiceByIdRequest : IRequest<ApiResponse<ServiceDto>>
{
    public GetServiceByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
