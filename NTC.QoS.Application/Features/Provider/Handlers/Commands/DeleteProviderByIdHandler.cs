using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class DeleteProviderByIdHandler(IRepository<Domain.Entities.Provider> repository) : IRequestHandler<DeleteProviderByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var provider = await _repository.FindAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);
        if (provider is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvider", [ "Provider not found."]
                    }
                },
                Message = "The provider you are trying to delete does not exist."
            };
        }

        _repository.Delete(provider);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        { 
            Data = true,
            Succeeded = true,
            Message = $"Provider '{provider.Name}' has been successfully deleted."
        };
    }
}
