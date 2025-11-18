using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetProviderLogoByIdHandler(
    IRepository<Domain.Entities.Provider> repository,
    ILogger<GetProviderLogoByIdHandler> logger)
    : IRequestHandler<GetProviderLogoByIdRequest, ApiResponse<ProviderLogoDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    private readonly ILogger<GetProviderLogoByIdHandler> _logger = logger;

    public async Task<ApiResponse<ProviderLogoDto>> Handle(GetProviderLogoByIdRequest request, CancellationToken cancellationToken)
    {
        var providerQuery = _repository.Query(p => p.Id == request.Id)
            .Select(p => new { p.LogoData, p.LogoContentType, p.LogoFileName });

        var provider = await _repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is null)
        {
            _logger.LogWarning("Provider with ID {ProviderId} not found", request.Id);
            return new ApiResponse<ProviderLogoDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "ProviderNotFound", new[] { "Provider not found." } }
                },
                Message = "The provider does not exist."
            };
        }

        if (provider.LogoData == null || provider.LogoData.Length == 0)
        {
            return new ApiResponse<ProviderLogoDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NoLogo", new[] { "Provider has no logo." } }
                },
                Message = "This provider does not have a logo."
            };
        }

        return new ApiResponse<ProviderLogoDto>
        {
            Succeeded = true,
            Message = "Provider logo retrieved successfully.",
            Data = new ProviderLogoDto
            {
                LogoData = provider.LogoData,
                ContentType = provider.LogoContentType ?? "application/octet-stream",
                FileName = provider.LogoFileName ?? "logo"
            }
        };
    }
}
