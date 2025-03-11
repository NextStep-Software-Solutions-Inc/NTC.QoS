using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class CreateServiceProviderHandler(IRepository<Domain.Aggregates.ServiceProvider> repository) : IRequestHandler<CreateServiceProviderRequest, ApiResponse<Domain.Aggregates.ServiceProvider>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _repository = repository;
    public async Task<ApiResponse<Domain.Aggregates.ServiceProvider>> Handle(CreateServiceProviderRequest request, CancellationToken cancellationToken)
    {
        var serviceProviderExists = await _repository.FindAsync(
    s => s.ServiceId == request.ServiceProvider.ServiceId
    && s.ProviderId == request.ServiceProvider.ProviderId
    && s.MNC == request.ServiceProvider.MNC,
    cancellationToken);

        if (serviceProviderExists != null)
        {
            if (!serviceProviderExists.IsDeleted)
            {
                return new ApiResponse<Domain.Aggregates.ServiceProvider>
                {
                    Succeeded = false,
                    Message = "Service Provider already exists.",
                    Error = new[] { "A Service Provider with the same name already exists." }
                };
            }
            else
            {
                serviceProviderExists.IsDeleted = false;
                _repository.Update(serviceProviderExists);
                await _repository.SaveChangesAsync(cancellationToken);

                return new ApiResponse<Domain.Aggregates.ServiceProvider>
                {
                    Succeeded = true,
                    Message = "Service Provider reactivated successfully.",
                    Data = serviceProviderExists
                };
            }
        }

        var serviceProvider = new Domain.Aggregates.ServiceProvider
        {
            ServiceId = request.ServiceProvider.ServiceId,
            ProviderId = request.ServiceProvider.ProviderId,
            MNC = request.ServiceProvider.MNC,
            MCC = request.ServiceProvider.MCC
        };
        _repository.Add(serviceProvider);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<Domain.Aggregates.ServiceProvider>
        {
            Succeeded = true,
            Message = "Service Provider created successfully",
            Data = serviceProvider
        };
    }
}
