using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Queries;

public class GetAllGenerationsHandler (IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<GetAllGenerationsRequest, PaginatedResponse<Domain.Aggregates.Generation>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;
    public async Task<PaginatedResponse<Domain.Aggregates.Generation>> Handle(GetAllGenerationsRequest request, CancellationToken cancellationToken)
    {
        var allGenerations = await _repository.ListAsync(g => g.IsDeleted != true, cancellationToken);

        return new PaginatedResponse<Domain.Aggregates.Generation>(allGenerations, request.PageNumber, request.PageSize);
    }
}