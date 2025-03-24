using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;
public class GetAllProvidersHandler(IRepository<Domain.Entities.Provider> repository) : IRequestHandler<GetAllProvidersRequest, PaginatedResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;

    public async Task<PaginatedResponse<ProviderDto>> Handle(GetAllProvidersRequest request, CancellationToken cancellationToken)
    {
        var allProviders = await _repository.ListAsync(p => p.IsDeleted != true, cancellationToken);

        var allProvidersDto = allProviders.Select(p => new ProviderDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            LicenseNumber = p.LicenseNumber,
            CreatedDate = p.CreatedAt,
            CreatedBy = p.CreatedBy,
            ModifiedDate = p.UpdatedAt,
            ModifiedBy = p.UpdatedBy,
        }).ToList();


        return new PaginatedResponse<ProviderDto>(allProvidersDto, request.PageNumber, request.PageSize);
    }
}

