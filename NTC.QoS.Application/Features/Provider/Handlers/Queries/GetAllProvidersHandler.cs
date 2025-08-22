using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Provider;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetAllProvidersHandler(
    IProviderRepository providerRepository,
    ILogger<GetAllProvidersHandler> logger) : IRequestHandler<GetAllProvidersRequest, PaginatedResponse<ProviderDto>>
{
    private readonly IProviderRepository _providerRepository = providerRepository;
    private readonly ILogger<GetAllProvidersHandler> _logger = logger;

    public async Task<PaginatedResponse<ProviderDto>> Handle(GetAllProvidersRequest request, CancellationToken cancellationToken)
    {
        var allProvidersQuery = _providerRepository.Query();
        
        allProvidersQuery = allProvidersQuery
            .Where(p =>  request.ServiceIds == null || request.ServiceIds.Count.Equals(0) || p.ServiceProviders.Any(sp => sp.Service != null && request.ServiceIds.Contains(sp.Service.Id)))
            .Where(p => request.Names == null || request.Names.Any(n => p.Name.Trim().ToLower().Contains(n.Trim().ToLower())));

        var shouldIncludeLogo = request.IncludeLogoData ?? false; // Default to false for list views
        
        if (shouldIncludeLogo)
        {
            _logger.LogDebug("Including logo data in provider list response (performance impact warning)");
            
            var providerDtosQuery = allProvidersQuery
                .OrderBy(p => p.CreatedAt)
                .Select(p => new ProviderDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    LicenseNumber = p.LicenseNumber,
                    Color = p.Color,
                    HasLogo = p.LogoData != null && p.LogoData.Length > 0,
                    LogoContentType = p.LogoContentType,
                    LogoFileName = p.LogoFileName,
                    LogoSizeBytes = p.LogoData != null ? p.LogoData.Length : null,
                    LogoUploadedAt = p.LogoUploadedAt,
                    LogoDataBase64 = p.LogoData != null && p.LogoData.Length > 0 
                        ? Convert.ToBase64String(p.LogoData) 
                        : null
                });

            return await _providerRepository.PaginateAsync(providerDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
        }
        else
        {
            _logger.LogDebug("Excluding logo data from provider list response for better performance");
            
            var providerDtosQuery = allProvidersQuery
                .OrderBy(p => p.CreatedAt)
                .Select(p => new ProviderDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    LicenseNumber = p.LicenseNumber,
                    Color = p.Color,
                    HasLogo = p.LogoData != null && p.LogoData.Length > 0,
                    LogoContentType = p.LogoContentType,
                    LogoFileName = p.LogoFileName,
                    LogoSizeBytes = p.LogoData != null ? p.LogoData.Length : null,
                    LogoUploadedAt = p.LogoUploadedAt,
                    LogoDataBase64 = null
                });

            return await _providerRepository.PaginateAsync(providerDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
        }
    }
}
