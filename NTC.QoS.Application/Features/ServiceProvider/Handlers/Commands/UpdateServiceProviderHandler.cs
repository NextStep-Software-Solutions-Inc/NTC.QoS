using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class UpdateServiceProviderHandler(IServiceProviderRepository repository) : IRequestHandler<UpdateServiceProviderByIdRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _repository = repository;

    public async Task<ApiResponse<ServiceProviderDto>> Handle(UpdateServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<ServiceProviderDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Service Provider not found."]  }
                },
                Message = "The service provider you are trying to edit does not exist."
            };
        }
        serviceProvider.MNC = request.ServiceProvider.MNC;
        serviceProvider.MCC = request.ServiceProvider.MCC;

        var serviceProviderExists = await _repository.FindAsync(sp => sp.ServiceId == serviceProvider.ServiceId
                                                                    && sp.ProviderId == serviceProvider.ProviderId
                                                                    && sp.MNC == serviceProvider.MNC
                                                                    && sp.MCC == serviceProvider.MCC,
                                                                    cancellationToken);
        if (serviceProviderExists is not null)
        {
            return new ApiResponse<ServiceProviderDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Service Provider", ["Service provider already exists."] }
                },
                Message = "Service provider already exists."
            };
        }

        _repository.Update(serviceProvider);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ServiceProviderDto>
        {
            Data = new ServiceProviderDto
            {
                Id = serviceProvider.Id,
                Provider = new ()
                {
                    Id = serviceProvider.ProviderId,
                    Name = serviceProvider.Provider?.Name ?? string.Empty,
                    Color = serviceProvider.Provider?.Color ?? string.Empty,
                    Description = serviceProvider.Provider?.Description ?? string.Empty,
                    LicenseNumber = serviceProvider.Provider?.LicenseNumber ?? string.Empty
                },
                Service = new ()
                {
                    Id = serviceProvider.ServiceId,
                    Name = serviceProvider.Service?.Name ?? string.Empty,
                    Type = serviceProvider.Service?.Type.ToString() ?? string.Empty
                },
                MNC = serviceProvider.MNC,
                MCC = serviceProvider.MCC
            },
            Succeeded = true,
            Message = "Service provider updated successfully."
        };
    }
}
