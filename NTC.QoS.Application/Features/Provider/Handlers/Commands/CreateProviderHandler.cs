using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class CreateProviderHandler(IRepository<Domain.Entities.Provider> repository) : IRequestHandler<CreateProviderRequest, ApiResponse<ProviderDto>>
{
    public async Task<ApiResponse<ProviderDto>> Handle(CreateProviderRequest request, CancellationToken cancellationToken)
    {
        var providerQueryAll = repository.QueryWithIgnoreQueryFilters(p => p.Name == request.Provider.Name);
        var provider = await repository.FirstOrDefaultAsync(providerQueryAll, cancellationToken);

        if (provider is not null && provider.IsDeleted)
        {
            var restoreQuery = repository.QueryWithIgnoreQueryFilters(p => p.Id == provider.Id && p.IsDeleted);
            await repository.RestoreAsync(restoreQuery, cancellationToken);

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

        if (provider is not null && !provider.IsDeleted)
        {
            return new ApiResponse<ProviderDto>
            {
                Message = "Provider already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Provider with the same name already exists."]
                    }
                }
            };
        }

        var newProvider = new Domain.Entities.Provider
        {
            Name = request.Provider.Name,
            Description = request.Provider.Description,
            LicenseNumber = request.Provider.LicenseNumber,
            Color = request.Provider.Color
        };

        repository.Add(newProvider);
        await repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ProviderDto>
        {
            Data = new ProviderDto
            {
                Id = newProvider.Id,
                Name = newProvider.Name,
                Description = newProvider.Description,
                LicenseNumber = newProvider.LicenseNumber,
                Color = newProvider.Color,
                HasLogo = newProvider.LogoData != null && newProvider.LogoData.Length > 0,
                LogoContentType = newProvider.LogoContentType,
                LogoFileName = newProvider.LogoFileName,
                LogoSizeBytes = newProvider.LogoData?.Length,
                LogoUploadedAt = newProvider.LogoUploadedAt
            },
            Message = "Provider created successfully.",
            Succeeded = true
        };
    }
}
