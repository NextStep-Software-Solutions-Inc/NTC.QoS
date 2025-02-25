using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Queries;
public class GetServiceByIdHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<GetServiceByIdRequest, ApiResponse<ServiceDto>>
{
    private readonly IRepository<Domain.Entities.Service> _repository = repository;
    public async Task<ApiResponse<ServiceDto>> Handle(GetServiceByIdRequest request, CancellationToken cancellationToken)
    {
        var service = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (service == null)
        {
            return new ApiResponse<ServiceDto>
            {
                Succeeded = false,
                Message = "Service not found.",
                Error = new[] { "Invalid service does not exist." }
            };
        }
        return new ApiResponse<ServiceDto>
        {
            Succeeded = true,
            Message = "Provider retrieved successfully",
            Data = new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Type = EnumHelper<ServiceTypeEnum>.GetName( service.Type),
                CreatedBy = service.CreatedBy,
                CreatedDate = service.CreatedAt,
                ModifiedBy = service.UpdatedBy,
                ModifiedDate = service.UpdatedAt
            }
        };
    }
}
