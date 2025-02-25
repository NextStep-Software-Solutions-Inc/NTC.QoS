using MediatR;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class UpdateServiceByIdRequest : IRequest<ApiResponse<UpdateServiceDto>>
{
    public UpdateServiceByIdRequest(Guid id, UpdateServiceDto updateService)
    {
        Id = id;
        Service = updateService;
    }
    public Guid Id { get; set; }
    public UpdateServiceDto Service { get; set; }
}
