using FluentValidation;
using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class UpdateProviderByIdHandler(IRepository<Domain.Entities.Provider> repository, IValidator<UpdateProviderByIdRequest> validator) : IRequestHandler<UpdateProviderByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    public async Task<ApiResponse<ProviderDto>> Handle(UpdateProviderByIdRequest request,
            CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (validationResult.IsValid.Equals(false))
        {
            return new ApiResponse<ProviderDto>
            {
                Message = "Validation failed",
                Succeeded = false,
                Errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage })
            };
        }
        var provider = await _repository.FindAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Provider", [ "The provider you are trying to edit does not exist."] }
                },
                Message = "The provider you are trying to edit does not exist."
            }; ;
        }
        provider.Description = request.Provider.Description;

         _repository.Update(provider);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<ProviderDto>
        {
            Succeeded = true,
            Data = new ProviderDto ()
            {
                Id = provider.Id,
                Description = provider.Description,
                Name = provider.Name
            },
            Message = "Request successful! Your changes have been saved.."
        };
    }
}