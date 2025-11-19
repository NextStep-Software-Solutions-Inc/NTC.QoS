using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Queries;

public class GetServiceByIdHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<GetServiceByIdRequest, ApiResponse<ServiceDto>>
{
    private readonly IRepository<Domain.Entities.Service> _repository = repository;

    public async Task<ApiResponse<ServiceDto>> Handle(GetServiceByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceQuery = _repository.Query(s => s.Id.Equals(request.Id));
        var service = await _repository.FirstOrDefaultAsync(serviceQuery, cancellationToken);
        if (service is null)
        {
            return new ApiResponse<ServiceDto>
            {
                Succeeded = false,
                Message = "Service does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullService", ["Service not found."]
                    }
                }
            };
        }

        return new ApiResponse<ServiceDto>
        {
            Succeeded = true,
            Message = "Service retrieved successfully.",
            Data = new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Type = service.Type.ToString()
            }
        };
    }
}
