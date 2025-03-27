using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Commands;

public class CreateServiceHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<CreateServiceRequest, ApiResponse<ServiceDto>>
{
    private readonly IRepository<Domain.Entities.Service> _repository = repository;

    public async Task<ApiResponse<ServiceDto>> Handle(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await _repository.FindAsync(s => s.Name == request.Service.Name
                                                       && s.Type == EnumHelper<ServiceTypeEnum>.ParseUserInput(request.Service.Type), cancellationToken);
        if (service is not null && service.IsDeleted.Equals(true))
        {
            service.IsDeleted = false;
            _repository.Update(service);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<ServiceDto>
            {
                Data = new ServiceDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Type = service.Type.ToString()
                },
                Message = "Service reactivated successfully.",
                Succeeded = true
            };
        }

        if (service is not null && service.IsDeleted.Equals(false))
        {
            return new ApiResponse<ServiceDto>
            {
                Message = "Provider already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Provider with the same name already exists."]
                    }
                }
            };
        }

        var newService = new Domain.Entities.Service
        {
            Name = request.Service.Name,
            Type = EnumHelper<ServiceTypeEnum>.ParseUserInput(request.Service.Type),
        };

        _repository.Add(newService);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ServiceDto>
        {
            Data = new ServiceDto
            {
                Id = newService.Id,
                Name = newService.Name,
                Type = newService.Type.ToString()
            },
            Message = "Service created successfully.",
            Succeeded = true
        };
    }
}
