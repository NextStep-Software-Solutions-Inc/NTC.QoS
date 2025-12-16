using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Queries;

public class GetAllGenerationsHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<GetAllGenerationsRequest, PaginatedResponse<GenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;
    public async Task<PaginatedResponse<GenerationDto>> Handle(GetAllGenerationsRequest request, CancellationToken cancellationToken)
    {
        var generationQuery = _repository.Query();

        generationQuery = generationQuery
            .Where(g => request.ProviderIds == null || g.GenerationBands.Any(gb => gb.Band.Links.Any(l => request.ProviderIds.Contains(l.ServiceProvider.ProviderId))))
            .Where(g => request.ServiceIds == null || g.GenerationBands.Any(gb => gb.Band.Links.Any(l => request.ServiceIds.Contains(l.ServiceProvider.ServiceId))))
            .Where(g => request.ServiceProviderIds == null || g.GenerationBands.Any(gb => gb.Band.Links.Any(l => request.ServiceProviderIds.Contains(l.ServiceProviderId))));

        var generationDtosQuery = generationQuery.Select(g => new GenerationDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
        });

        return await _repository.PaginateAsync(generationDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
