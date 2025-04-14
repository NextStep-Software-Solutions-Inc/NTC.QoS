using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Queries;

public class GetAllServicesHandler(IServiceRepository repository) : IRequestHandler<GetAllServicesRequest, PaginatedResponse<ServiceDto>>
{
    private readonly IServiceRepository _repository = repository;

    public async Task<PaginatedResponse<ServiceDto>> Handle(GetAllServicesRequest request, CancellationToken cancellationToken)
    {
        var allServices = await _repository.ListAsync(s => !s.IsDeleted, cancellationToken);

        if (request.ProviderIds is not null && request.ProviderIds.Count > 0)
        {
            allServices = allServices
                        .Where(s => s.ServiceProviders.Any(sp => sp.Provider is not null && request.ProviderIds.Contains(sp.Provider.Id))).ToList();
        }

        allServices = allServices
            .Where(s => string.IsNullOrEmpty(request.Type) || s.Type == EnumHelper<ServiceTypeEnum>.ParseUserInput(request.Type)).ToList();

        var serviceDtos = allServices.Select(service => new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Type =service.Type.ToString()
        }).ToList();

        if (serviceDtos.Count is 0)
        {
            return new PaginatedResponse<ServiceDto>(serviceDtos, request.PageNumber, request.PageSize)
            {
                Succeeded = false,
                Message = "No records found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullServices", ["No services found."]
                    }
                }
            };
        }

        return new PaginatedResponse<ServiceDto>(serviceDtos, request.PageNumber, request.PageSize)
        {
            Data = serviceDtos,
            Succeeded = true,
            Message = "Services retrieved successfully."
        };
    }
}
