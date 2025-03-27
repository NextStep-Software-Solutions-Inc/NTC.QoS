using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class CreateServiceRequest(CreateServiceDto service) : IRequest<ApiResponse<ServiceDto>>
{
    public CreateServiceDto Service { get; set; } = service;
}
