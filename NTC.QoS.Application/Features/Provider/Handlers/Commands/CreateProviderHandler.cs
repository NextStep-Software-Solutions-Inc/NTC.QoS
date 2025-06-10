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
        var providerQuery =  repository.Query(p => p.Name == request.Provider.Name);
        var provider = await repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is not null && provider.IsDeleted) 
        {
            provider.IsDeleted = false;
            repository.Update(provider);
            await repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<ProviderDto>
            {
                Data = new ProviderDto
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    Description = provider.Description,
                    LicenseNumber = provider.LicenseNumber,
                    Color = provider.Color
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
                Color = newProvider.Color
            },
            Message = "Provider created successfully.",
            Succeeded = true
        };
    }
}
