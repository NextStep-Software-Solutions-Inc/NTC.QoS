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
        var allProviders = await _repository.GetAll(request.PageNumber, request.PageSize, cancellationToken);
        return new PaginatedResponse<ProviderDto>(request.PageNumber, request.PageSize, allProviders.TotalRows);
    }

}

