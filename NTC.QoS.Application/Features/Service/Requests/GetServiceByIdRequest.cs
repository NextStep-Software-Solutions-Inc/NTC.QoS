using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class GetServiceByIdRequest(Guid id) : IRequest<ApiResponse<ServiceDto>>
{
    public Guid Id { get; set; } = id;
}
