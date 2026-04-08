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
        var providerQuery = repository.Query(p => p.Name == request.Provider.Name);
        var provider = await repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is not null)
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

        var deletedProviderQuery = repository.QueryWithIgnoreQueryFilters(p => p.Name == request.Provider.Name && p.IsDeleted);
        var deletedProvider = await repository.FirstOrDefaultAsync(deletedProviderQuery, cancellationToken);

        if (deletedProvider is not null)
        {
            return new ApiResponse<ProviderDto>
            {
                Message = $"A deleted provider with the name '{request.Provider.Name}' already exists (Id: {deletedProvider.Id}). Use POST /providers/{deletedProvider.Id}/restore to reactivate it.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "DeletedProviderExists", [$"A soft-deleted provider with this name exists (Id: {deletedProvider.Id})."]
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
