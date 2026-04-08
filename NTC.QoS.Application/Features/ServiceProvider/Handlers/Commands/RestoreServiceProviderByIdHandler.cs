using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class RestoreServiceProviderByIdHandler(IServiceProviderRepository repository) : IRequestHandler<RestoreServiceProviderByIdRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _repository = repository;

    public async Task<ApiResponse<ServiceProviderDto>> Handle(RestoreServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProviderQuery = _repository.QueryWithIgnoreQueryFilters(sp => sp.Id == request.Id && sp.IsDeleted);
        var serviceProvider = await _repository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);

        if (serviceProvider is null)
        {
            return new ApiResponse<ServiceProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullServiceProvider", ["Service Provider not found or is not deleted."] }
                },
                Message = "The service provider you are trying to restore does not exist or is already active."
            };
        }

        var conflictingServiceProviderQuery = _repository.QueryWithIgnoreQueryFilters(
            sp => sp.ServiceId == serviceProvider.ServiceId
                  && sp.ProviderId == serviceProvider.ProviderId
                  && sp.MNC == serviceProvider.MNC
                  && sp.Id != serviceProvider.Id
                  && !sp.IsDeleted);
        var conflictingServiceProvider = await _repository.FirstOrDefaultAsync(conflictingServiceProviderQuery, cancellationToken);

        if (conflictingServiceProvider is not null)
        {
            return new ApiResponse<ServiceProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "DuplicateServiceProvider", [$"An active service provider with the same ServiceId, ProviderId, and MNC already exists."] }
                },
                Message = "The service provider cannot be restored because another active service provider already uses the same ServiceId, ProviderId, and MNC combination."
            };
        }

        serviceProvider.IsDeleted = false;
        serviceProvider.DeletedAt = null;
        serviceProvider.DeletedBy = null;
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
                    Color = serviceProvider.Provider?.Color ?? string.Empty,
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
            Message = "Service Provider restored successfully.",
            Succeeded = true
        };
    }
}
