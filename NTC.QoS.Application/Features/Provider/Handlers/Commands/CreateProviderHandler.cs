using FluentValidation;
using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class CreateProviderHandler(IRepository<Domain.Entities.Provider> repository, IValidator<CreateProviderRequest> validator) : IRequestHandler<CreateProviderRequest, ApiResponse<CreateProviderDto>>
{
    public async Task<ApiResponse<CreateProviderDto>> Handle(CreateProviderRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (validationResult.IsValid.Equals(false))
        {
            return new ApiResponse<CreateProviderDto>
            {
                Message = "Validation failed",
                Succeeded = false,
                Errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage })
            };
        }

        var provider =  await repository.FindAsync(p => p.Name == request.Provider.Name, cancellationToken);

        if (provider is not null && provider.IsDeleted.Equals(true)) 
        {
            provider.IsDeleted = false;
            repository.Update(provider);
            await repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<CreateProviderDto>
            {
                Data = new CreateProviderDto
                {
                    Name = provider.Name,
                    Description = provider.Description,
                    LicenseNumber = provider.LicenseNumber
                },
                Message = "Provider restored successfully",
                Succeeded = true
            };
        }

        if (provider is not null && provider.IsDeleted.Equals(false))
        {
            return new ApiResponse<CreateProviderDto>
            {
                Message = "Provider already exists",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error", ["Provider with the same name already exists."] }
                }
            };
        }

        var newProvider = new Domain.Entities.Provider
        {
            Name = request.Provider.Name,
            Description = request.Provider.Description,
            LicenseNumber = request.Provider.LicenseNumber
        };

        repository.Add(newProvider);
        await repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateProviderDto>
        {
            Data = new CreateProviderDto
            {
                Name = newProvider.Name,
                Description = newProvider.Description,
                LicenseNumber = newProvider.LicenseNumber
            },
            Message = "Provider created successfully",
            Succeeded = true
        };
    }
}