using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class DeleteServiceProviderHandler(IRepository<Domain.Aggregates.ServiceProvider> repository) : IRequestHandler<DeleteServiceProviderByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProviderQuery = _repository.Query(sp => sp.Id == request.Id);
        var serviceProvider = await _repository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullServiceProvider", ["Service Provider not found."]
                    }
                },
                Message = "The service provider you are trying to delete does not exist."
            };
        }

        _repository.Delete(serviceProvider);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "Service Provider deleted successfully."
        };
    }
}
