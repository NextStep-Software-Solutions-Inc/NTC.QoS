using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Commands;

public class UpdateServiceByIdHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<UpdateServiceByIdRequest, ApiResponse<ServiceDto>>
{
    private readonly IRepository<Domain.Entities.Service> _repository = repository;

    public async Task<ApiResponse<ServiceDto>> Handle(UpdateServiceByIdRequest request, CancellationToken cancellationToken)
    {
        var service = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (service is null)
        {
            return new ApiResponse<ServiceDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullService", ["Service not found."]
                    }
                },
                Message = "The service you are trying to delete does not exist."
            };
        }
        service.Name = request.Service.Name;
        service.Type = EnumHelper<ServiceTypeEnum>.ParseUserInput(request.Service.Type);

        _repository.Update(service);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ServiceDto>
        {
            Data = new ServiceDto
            {
                Id = service.Id,
                Name = service.Name ?? string.Empty,
                Type = service.Type.ToString()
            },
            Message = "Service updated successfully.",
            Succeeded = true
        };
    }
}
