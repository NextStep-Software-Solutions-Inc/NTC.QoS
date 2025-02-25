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
        var allGenerations = await _repository.ListAsync(g => g.IsDeleted != true, cancellationToken);

        var generationDtos = allGenerations.Select(g => new GenerationDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            CreatedBy = g.CreatedBy,
            CreatedDate = g.CreatedAt,
            ModifiedBy = g.UpdatedBy,
        }).ToList();

        return new PaginatedResponse<GenerationDto>(generationDtos, request.PageNumber, request.PageSize);
    }
}