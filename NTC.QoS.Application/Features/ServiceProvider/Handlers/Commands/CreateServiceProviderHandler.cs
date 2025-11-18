using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class CreateServiceProviderHandler(IServiceProviderRepository repository) : IRequestHandler<CreateServiceProviderRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _repository = repository;

    public async Task<ApiResponse<ServiceProviderDto>> Handle(CreateServiceProviderRequest request, CancellationToken cancellationToken)
    {
        var serviceProviderQuery = _repository.QueryWithIgnoreQueryFilters(sp => sp.ServiceId == request.ServiceProvider.ServiceId
                                                                    && sp.ProviderId == request.ServiceProvider.ProviderId
                                                                    && sp.MNC == request.ServiceProvider.MNC
                                                                    && sp.MCC == request.ServiceProvider.MCC);
        var serviceProvider = await _repository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is not null && serviceProvider.IsDeleted)
        {
            serviceProvider.IsDeleted = false;
            _repository.Update(serviceProvider);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<ServiceProviderDto>
            {
                Data = new ServiceProviderDto
                {
                    Id = serviceProvider.Id,
                    Provider = new()
                    {
                        Id = serviceProvider.ProviderId,
                        Name = serviceProvider.Provider?.Name ?? string.Empty,
                        Color = serviceProvider.Provider?.Color ?? string.Empty,
                        Description = serviceProvider.Provider?.Description ?? string.Empty,
                        LicenseNumber = serviceProvider.Provider?.LicenseNumber ?? string.Empty
                    },
                    Service = new()
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

        if (serviceProvider is not null && !serviceProvider.IsDeleted)
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
                Provider = new()
                {
                    Id = newServiceProvider.ProviderId,
                    Name = newServiceProvider.Provider?.Name ?? string.Empty,
                    Color = newServiceProvider.Provider?.Color ?? string.Empty,
                    Description = newServiceProvider.Provider?.Description ?? string.Empty,
                    LicenseNumber = newServiceProvider.Provider?.LicenseNumber ?? string.Empty
                },
                Service = new()
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
