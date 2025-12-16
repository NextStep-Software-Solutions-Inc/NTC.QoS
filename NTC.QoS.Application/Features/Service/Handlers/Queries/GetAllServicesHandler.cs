using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Queries;

public class GetAllServicesHandler(IServiceRepository repository) : IRequestHandler<GetAllServicesRequest, PaginatedResponse<ServiceDto>>
{
    private readonly IServiceRepository _repository = repository;

    public async Task<PaginatedResponse<ServiceDto>> Handle(GetAllServicesRequest request, CancellationToken cancellationToken)
    {
        var allServicesQuery = _repository.Query();

        if (request.ProviderIds is not null && request.ProviderIds.Count > 0)
        {
            allServicesQuery = allServicesQuery
                        .Where(s => s.ServiceProviders.Any(sp => request.ProviderIds.Contains(sp.Provider.Id)));
        }

        if (request.Types is not null && request.Types.Count > 0)
        {
            var validTypes = request.Types
                .Select(type => EnumHelper<ServiceTypeEnum>.ParseUserInput(type))
                .Where(parsedType => parsedType != ServiceTypeEnum.Unknown);

            if (validTypes.Any())
            {
                allServicesQuery = allServicesQuery.Where(s => validTypes.Contains(s.Type));
            }
        }

        var serviceDtosQuery = allServicesQuery
            .OrderBy(s => s.CreatedAt)
            .Select(service => new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Type = service.Type.ToString()
            });

        return await _repository.PaginateAsync(serviceDtosQuery, request.PageNumber, request.PageSize, cancellationToken);

    }
}
