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

        var totalRows = allProviders.Count;

        var paginatedProviders = allProviders
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var providerDtos = paginatedProviders.Select(p => new ProviderDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            LicenseNumber = p.LicenseNumber,
            CreatedBy = p.CreatedBy,
            CreatedDate = p.CreatedAt,
            ModifiedBy = p.UpdatedBy,
            ModifiedDate = p.UpdatedAt,
            IsDeleted   = p.IsDeleted
        }).ToList();

        return new PaginatedResponse<ProviderDto>(providerDtos, request.PageNumber, request.PageSize, totalRows);
    }
}

