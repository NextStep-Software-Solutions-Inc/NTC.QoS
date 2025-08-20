using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class UpdateServiceByIdRequest(Guid id, UpdateServiceDto service) : IRequest<ApiResponse<ServiceDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateServiceDto Service { get; set; } = service;
}
