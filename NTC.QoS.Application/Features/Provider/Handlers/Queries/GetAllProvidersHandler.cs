using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Provider;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

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
            .Where(p => request.ServiceIds == null || request.ServiceIds.Count.Equals(0) || p.ServiceProviders.Any(sp => sp.Service != null && request.ServiceIds.Contains(sp.Service.Id)))
            .Where(p => request.Names == null || request.Names.Any(n => p.Name.Trim().ToLower().Contains(n.Trim().ToLower())));

        if (!string.IsNullOrEmpty(request.Search))
        {
            allProvidersQuery = _providerRepository.ApplySearch(allProvidersQuery, request.Search, p => p.Name);
        }

        // Performance optimization: Only include logo data if explicitly requested
        var shouldIncludeLogo = request.IncludeLogoData ?? false; // Default to false for list views

        _logger.LogDebug("Processing provider list request with logo data inclusion: {IncludeLogo}", shouldIncludeLogo);

        var providerDtosQuery = allProvidersQuery
            .OrderBy(p => p.CreatedAt)
            .Select(CreateProviderDtoProjection(shouldIncludeLogo));

        return await _providerRepository.PaginateAsync(providerDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }

    /// <summary>
    /// Creates a LINQ projection expression for mapping Provider entities to ProviderDto.
    /// Centralizes the mapping logic to avoid code duplication and improve maintainability.
    /// </summary>
    /// <param name="includeLogoData">Whether to include base64 logo data in the projection</param>
    /// <returns>Expression for LINQ Select operation</returns>
    private static Expression<Func<Domain.Entities.Provider, ProviderDto>> CreateProviderDtoProjection(bool includeLogoData)
    {
        return p => new ProviderDto
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
            // Conditionally include logo data based on performance requirements
            LogoDataBase64 = includeLogoData && p.LogoData != null && p.LogoData.Length > 0
                ? Convert.ToBase64String(p.LogoData)
                : null
        };
    }
}
