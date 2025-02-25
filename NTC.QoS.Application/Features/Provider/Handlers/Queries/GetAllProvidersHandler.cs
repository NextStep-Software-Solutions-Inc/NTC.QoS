using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;
public class GetAllProvidersHandler(IRepository<Domain.Entities.Provider> repository) : IRequestHandler<GetAllProvidersRequest, PaginatedResponse<Domain.Entities.Provider>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;

    public async Task<PaginatedResponse<Domain.Entities.Provider>> Handle(GetAllProvidersRequest request, CancellationToken cancellationToken)
    {
        var allProviders = await _repository.ListAsync(p => p.IsDeleted != true, cancellationToken);


        return new PaginatedResponse<Domain.Entities.Provider>(allProviders, request.PageNumber, request.PageSize);
    }
}

