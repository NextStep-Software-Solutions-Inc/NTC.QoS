using MediatR;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class CreateServiceRequest : IRequest<ApiResponse<CreateServiceDto>>
{
    public CreateServiceRequest(CreateServiceDto service)
    {
        Service = service;
    }
    public CreateServiceDto Service { get; set; }
}
