using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetAllProvidersHandler(IProviderRepository repository) : IRequestHandler<GetAllProvidersRequest, PaginatedResponse<ProviderDto>>
{
    private readonly IProviderRepository _repository = repository;

    public async Task<PaginatedResponse<ProviderDto>> Handle(GetAllProvidersRequest request, CancellationToken cancellationToken)
    {
        var allProviders = await _repository.ListAsync(p => !p.IsDeleted, cancellationToken);

        if (request.ServiceIds is not null && request.ServiceIds.Count > 0)
        {
            allProviders = [.. allProviders
                        .Where(p => p.ServiceProviders.Any(sp => sp.Service is not null && request.ServiceIds.Contains(sp.Service.Id)))];
        }

        if (request.Names is not null && request.Names.Count > 0)
        {
            var Names = request.Names.Select(n => n.Trim().ToLower()).ToList();
            allProviders = [.. allProviders.Where(p => Names.Contains(p.Name.Trim().ToLower()))];
        }

        var providerDtos = allProviders.Select(p => new ProviderDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            LicenseNumber = p.LicenseNumber,
            Color = p.Color
        }).ToList();

        if (providerDtos.Count is 0)
        {
            return new PaginatedResponse<ProviderDto>(providerDtos, request.PageNumber, request.PageSize)
            {
                Succeeded = false,
                Message = "No records found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProviders", ["No providers found."]
                    }
                }
            };
        }

        return new PaginatedResponse<ProviderDto>(providerDtos, request.PageNumber, request.PageSize)
        {
            Data = providerDtos,
            Succeeded = true,
            Message = "Providers retrieved successfully."
        };
    }
}
