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
        var serviceProviderQuery = _repository.Query(sp => sp.ServiceId == request.ServiceProvider.ServiceId
                                                        && sp.ProviderId == request.ServiceProvider.ProviderId
                                                        && sp.MNC == request.ServiceProvider.MNC);
        var serviceProvider = await _repository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is not null)
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

        var deletedServiceProviderQuery = _repository.QueryWithIgnoreQueryFilters(
            sp => sp.ServiceId == request.ServiceProvider.ServiceId
               && sp.ProviderId == request.ServiceProvider.ProviderId
               && sp.MNC == request.ServiceProvider.MNC
               && sp.IsDeleted);
        var deletedServiceProvider = await _repository.FirstOrDefaultAsync(deletedServiceProviderQuery, cancellationToken);

        if (deletedServiceProvider is not null)
        {
            return new ApiResponse<ServiceProviderDto>
            {
                Message = $"A deleted service provider with this ServiceId/ProviderId/MNC combination already exists (Id: {deletedServiceProvider.Id}). Use POST /service-providers/{deletedServiceProvider.Id}/restore to reactivate it.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "DeletedServiceProviderExists", [$"A soft-deleted service provider with this ServiceId/ProviderId/MNC combination exists (Id: {deletedServiceProvider.Id})."]
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
