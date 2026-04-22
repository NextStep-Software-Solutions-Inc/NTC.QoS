using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class RestoreProviderByIdHandler(IRepository<Domain.Entities.Provider> repository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<RestoreProviderByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<ProviderDto>> Handle(RestoreProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var providerQuery = _repository.QueryWithIgnoreQueryFilters(p => p.Id == request.Id && p.IsDeleted);
        var provider = await _repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is null)
        {
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullProvider", ["Provider not found or is not deleted."] }
                },
                Message = "The provider you are trying to restore does not exist or is already active."
            };
        }

        var conflictingProviderQuery = _repository.QueryWithIgnoreQueryFilters(
            p => p.Name.ToLower() == provider.Name.ToLower() && p.Id != provider.Id && !p.IsDeleted);
        var conflictingProvider = await _repository.FirstOrDefaultAsync(conflictingProviderQuery, cancellationToken);

        if (conflictingProvider is not null)
        {
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "DuplicateProviderName", [$"An active provider with the name '{provider.Name}' already exists."] }
                },
                Message = "The provider cannot be restored because another active provider already uses this name."
            };
        }
        await _repository.RestoreAsync(providerQuery, cancellationToken);

        // Cascade restore: restore all soft-deleted service providers that belong to this provider
        var serviceProviderRestoreQuery = _serviceProviderRepository.QueryWithIgnoreQueryFilters(
            sp => sp.ProviderId == provider.Id && sp.IsDeleted);
        await _serviceProviderRepository.RestoreAsync(serviceProviderRestoreQuery, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ProviderDto>
        {
            Data = new ProviderDto
            {
                Id = provider.Id,
                Name = provider.Name,
                Description = provider.Description,
                LicenseNumber = provider.LicenseNumber,
                Color = provider.Color,
                HasLogo = provider.LogoData != null && provider.LogoData.Length > 0,
                LogoContentType = provider.LogoContentType,
                LogoFileName = provider.LogoFileName,
                LogoSizeBytes = provider.LogoData?.Length,
                LogoUploadedAt = provider.LogoUploadedAt
            },
            Message = "Provider restored successfully.",
            Succeeded = true
        };
    }
}
