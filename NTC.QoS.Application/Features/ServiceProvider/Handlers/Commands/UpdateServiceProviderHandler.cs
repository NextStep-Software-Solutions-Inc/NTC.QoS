using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class UpdateServiceProviderHandler(IRepository<Domain.Aggregates.ServiceProvider> repository) : IRequestHandler<UpdateServiceProviderByIdRequest, ApiResponse<Domain.Aggregates.ServiceProvider>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _repository = repository;
    public async Task<ApiResponse<Domain.Aggregates.ServiceProvider>> Handle(UpdateServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _repository.FindAsync(sp => sp.Id == request.Id && !sp.IsDeleted, cancellationToken);
        if (serviceProvider == null)
        {
            return new ApiResponse<Domain.Aggregates.ServiceProvider>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Service provider Not found." },
                Message = "The service provider you are trying to edit does not exist."
            };
        }
        serviceProvider.MNC = request.ServiceProvider.MNC;
        serviceProvider.MCC = request.ServiceProvider.MCC;

        _repository.Update(serviceProvider);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<Domain.Aggregates.ServiceProvider>
        {
            Succeeded = true,
            Data = serviceProvider,
            Message = "Request successful! Your changes have been saved."
        };
    }
}