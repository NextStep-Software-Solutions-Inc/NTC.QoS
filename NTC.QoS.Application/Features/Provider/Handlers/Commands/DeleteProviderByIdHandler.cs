using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class DeleteProviderByIdHandler(IRepository<Domain.Entities.Provider> repository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<DeleteProviderByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<bool>> Handle(DeleteProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var providerQuery = _repository.Query(p => p.Id == request.Id);
        var provider = await _repository.FirstOrDefaultAsync(providerQuery, cancellationToken);
        if (provider is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvider", [ "Provider not found."]
                    }
                },
                Message = "The provider you are trying to delete does not exist."
            };
        }

        // Cascade soft-delete all service providers belonging to this provider.
        // SaveChangesAsync must be called before Delete(provider) to avoid EF's cascade
        // overwriting the Modified state with Deleted, which the SoftDeleteInterceptor
        // would then revert to Unchanged (since IsDeleted is already true), losing the save.
        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.ProviderId == request.Id);
        await _serviceProviderRepository.SoftDeleteAsync(serviceProviderQuery, cancellationToken);
        await _serviceProviderRepository.SaveChangesAsync(cancellationToken);

        _repository.Delete(provider);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = "Provider deleted successfully."
        };
    }
}
