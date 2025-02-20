using MediatR;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;
public class GetAllRegionsHandler(IRepository<Domain.Entities.Region> repository) : IRequestHandler<GetAllRegionsRequest, PaginatedResponse<Domain.Entities.Region>>
{
    private readonly IRepository<Domain.Entities.Region> _repository = repository;

    public async Task<PaginatedResponse<Domain.Entities.Region>> Handle(GetAllRegionsRequest request, CancellationToken cancellationToken)
    {

        var allRegions = await _repository.ListAsync(p => !p.IsDeleted, cancellationToken);

        return new PaginatedResponse<Domain.Entities.Region>(allRegions, request.PageNumber, request.PageSize);
    }

}

