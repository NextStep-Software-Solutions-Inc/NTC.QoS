using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class DeleteProviderLogoByIdHandler(
    IRepository<Domain.Entities.Provider> repository,
    ILogger<DeleteProviderLogoByIdHandler> logger)
    : IRequestHandler<DeleteProviderLogoByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    private readonly ILogger<DeleteProviderLogoByIdHandler> _logger = logger;

    public async Task<ApiResponse<ProviderDto>> Handle(DeleteProviderLogoByIdRequest request, CancellationToken cancellationToken)
    {
        // Find the provider
        var providerQuery = _repository.Query(p => p.Id == request.Id);
        var provider = await _repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is null)
        {
            _logger.LogWarning("Provider with ID {ProviderId} not found", request.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "ProviderNotFound", new[] { "Provider not found." } }
                },
                Message = "The provider you are trying to update does not exist."
            };
        }

        if (provider.LogoData == null || provider.LogoData.Length == 0)
        {
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NoLogo", new[] { "Provider has no logo to delete." } }
                },
                Message = "This provider does not have a logo."
            };
        }

        try
        {
            _logger.LogInformation("Deleting logo for provider {ProviderId}", provider.Id);

            // Remove logo data and metadata
            provider.LogoData = null;
            provider.LogoContentType = null;
            provider.LogoFileName = null;
            provider.LogoUploadedAt = null;

            _repository.Update(provider);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<ProviderDto>
            {
                Succeeded = true,
                Data = new ProviderDto
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    Description = provider.Description,
                    LicenseNumber = provider.LicenseNumber,
                    Color = provider.Color,
                    HasLogo = false,
                    LogoContentType = null,
                    LogoFileName = null,
                    LogoSizeBytes = null,
                    LogoUploadedAt = null
                },
                Message = "Provider logo deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when deleting logo for provider {ProviderId}", provider.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "UnexpectedError", new[] { "An unexpected error occurred while deleting the logo." } }
                },
                Message = "An unexpected error occurred while deleting the logo."
            };
        }
    }
}