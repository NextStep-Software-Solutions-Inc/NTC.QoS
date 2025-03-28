using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class CreateServiceProviderHandler(IServiceProviderRepository repository) : IRequestHandler<CreateServiceProviderRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _repository = repository;

    public async Task<ApiResponse<ServiceProviderDto>> Handle(CreateServiceProviderRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _repository.FindAsync(sp => sp.ServiceId == request.ServiceProvider.ServiceId
                                                                    && sp.ProviderId == request.ServiceProvider.ProviderId
                                                                    && sp.MNC == request.ServiceProvider.MNC
                                                                    && sp.MCC == request.ServiceProvider.MCC,
                                                                    cancellationToken);
        if (serviceProvider is not null && serviceProvider.IsDeleted.Equals(true))
        {
            serviceProvider.IsDeleted = false;
            _repository.Update(serviceProvider);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<ServiceProviderDto>
            {
                Data = new ServiceProviderDto
                {
                    Id = serviceProvider.Id,
                    Provider = new ProviderDto
                    {
                        Id = serviceProvider.ProviderId,
                        Name = serviceProvider.Provider?.Name ?? string.Empty,
                        Description = serviceProvider.Provider?.Description ?? string.Empty,
                        LicenseNumber = serviceProvider.Provider?.LicenseNumber ?? string.Empty
                    },
                    Service = new ServiceDto
                    {
                        Id = serviceProvider.ServiceId,
                        Name = serviceProvider.Service?.Name ?? string.Empty,
                        Type = serviceProvider.Service?.Type.ToString() ?? string.Empty
                    },
                    MNC = serviceProvider.MNC,
                    MCC = serviceProvider.MCC
                },
                Message = "Service reactivated successfully.",
                Succeeded = true
            };
        }

        if (serviceProvider is not null && serviceProvider.IsDeleted.Equals(false))
        {
            return new ApiResponse<ServiceProviderDto>
            {
                Message = "Service provider already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Service provider already exists."]
                    }
                }
            };
        }

        var newServiceProvider = new Domain.Aggregates.ServiceProvider
        {
            ServiceId = request.ServiceProvider.ServiceId,
            ProviderId = request.ServiceProvider.ProviderId,
            MNC = request.ServiceProvider.MNC,
            MCC = request.ServiceProvider.MCC
        };

        _repository.Add(newServiceProvider);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ServiceProviderDto>
        {
            Data = new ServiceProviderDto
            {
                Id = newServiceProvider.Id,
                Provider = new ProviderDto
                {
                    Id = newServiceProvider.ProviderId,
                    Name = newServiceProvider.Provider?.Name ?? string.Empty,
                    Description = newServiceProvider.Provider?.Description ?? string.Empty,
                    LicenseNumber = newServiceProvider.Provider?.LicenseNumber ?? string.Empty
                },
                Service = new ServiceDto
                {
                    Id = newServiceProvider.ServiceId,
                    Name = newServiceProvider.Service?.Name ?? string.Empty,
                    Type = newServiceProvider.Service?.Type.ToString() ?? string.Empty
                },
                MNC = newServiceProvider.MNC,
                MCC = newServiceProvider.MCC
            },
            Succeeded = true,
            Message = "Service Provider created successfully.",
        };
    }
}
