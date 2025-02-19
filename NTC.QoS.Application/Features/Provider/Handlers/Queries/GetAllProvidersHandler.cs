using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;
public class GetAllProvidersHandler : IRequestHandler<GetAllProvidersRequest, PaginatedResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository;

    public GetAllProvidersHandler(IRepository<Domain.Entities.Provider> repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResponse<ProviderDto>> Handle(GetAllProvidersRequest request, CancellationToken cancellationToken)
    {
        // Assuming the repository has built-in pagination support
        var paginatedProviders = await _repository.AddAsync(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken
        );

        // The repository should automatically handle the mapping and pagination.
        // The result is already in the form of PaginatedResponse<ProviderDto>
        return paginatedProviders;
    }
}

