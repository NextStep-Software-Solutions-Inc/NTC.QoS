using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetProviderByIdHandler(IRepository<Domain.Entities.Provider> repository) : IRequestHandler<GetProviderByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;

    public async Task<ApiResponse<ProviderDto>> Handle(GetProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var provider = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (provider is null)
        {
            return new ApiResponse<ProviderDto>
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

        return new ApiResponse<ProviderDto>
        {
            Succeeded = true,
            Message = "Provider retrieved successfully.",
            Data = new ProviderDto()
            {
                Id = provider.Id,
                Name = provider.Name,
                Description = provider.Description,
                LicenseNumber = provider.LicenseNumber,
                Color = provider.Color
            }
        };
    }
}

