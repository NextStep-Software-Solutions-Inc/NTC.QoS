using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetAllProvidersHandler(IProviderRepository providerRepository) : IRequestHandler<GetAllProvidersRequest, PaginatedResponse<ProviderDto>>
{
    private readonly IProviderRepository _providerRepository = providerRepository;

    public async Task<PaginatedResponse<ProviderDto>> Handle(GetAllProvidersRequest request, CancellationToken cancellationToken)
    {
        var allProvidersQuery = _providerRepository.Query();
        
        allProvidersQuery = allProvidersQuery
            .Where(p => request.ServiceIds == null || request.ServiceIds.Count.Equals(0) || p.ServiceProviders.Any(sp => sp.Service != null && request.ServiceIds.Contains(sp.Service.Id)));

        allProvidersQuery = allProvidersQuery
            .Where(p => request.Names == null || request.Names.Any(n => p.Name.Trim().Contains(n.Trim(), StringComparison.CurrentCultureIgnoreCase)));

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
                LogoUploadedAt = p.LogoUploadedAt
            });

        return await _providerRepository.PaginateAsync(providerDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
