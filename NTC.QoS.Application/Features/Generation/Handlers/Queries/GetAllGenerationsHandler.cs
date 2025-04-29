using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Queries;

public class GetAllGenerationsHandler (IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<GetAllGenerationsRequest, PaginatedResponse<GenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;
    public async Task<PaginatedResponse<GenerationDto>> Handle(GetAllGenerationsRequest request, CancellationToken cancellationToken)
    {
        var generationQuery = _repository.Query(g => g.IsDeleted.Equals(false));

        generationQuery = generationQuery
            .Where(g => request.Providers == null || g.GenerationBands.Any(gb => gb.Band.Links.Any(l => request.Providers.Contains(l.ServiceProvider.ProviderId))))
            .Where(g => request.Services == null || g.GenerationBands.Any(gb => gb.Band.Links.Any(l => request.Services.Contains(l.ServiceProvider.ServiceId))))
            .Where(g => request.ServiceProviders == null || g.GenerationBands.Any(gb => gb.Band.Links.Any(l => request.ServiceProviders.Contains(l.ServiceProviderId))));

        var generationDtosQuery = generationQuery.Select(g => new GenerationDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            CreatedBy = g.CreatedBy,
            CreatedDate = g.CreatedAt,
            ModifiedBy = g.UpdatedBy,
            ModifiedDate = g.UpdatedAt
        });

        return await _repository.PaginateAsync(generationDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
