using MediatR;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Queries;

public class GetAllGenerationBandsHandler (IRepository<Domain.Aggregates.GenerationBand> repository) : IRequestHandler<GetAllGenerationBandsRequest, PaginatedResponse<GetGenerationBandDto>>
{
    private readonly IRepository<Domain.Aggregates.GenerationBand> _repository = repository;
    public async Task<PaginatedResponse<GetGenerationBandDto>> Handle(GetAllGenerationBandsRequest request, CancellationToken cancellationToken)
    {
        var allGenerationBands = await _repository.ListAsync(gb => !gb.IsDeleted, cancellationToken);

        if (request.GenerationId.HasValue)
        {
            allGenerationBands = [.. allGenerationBands.Where(gb => gb.GenerationId == request.GenerationId.Value)];
        }

        if (request.BandId.HasValue)
        {
            allGenerationBands = [.. allGenerationBands.Where(gb => gb.BandId == request.BandId.Value)];
        }

        var generationBands = allGenerationBands.Select(gb => new GetGenerationBandDto
        {
            Id = gb.Id,
            Generation = new GenerationBandDto
            {
                Id = gb.GenerationId,
                Name = gb.Generation?.Name ?? "Unknown",
                Description = gb.Generation?.Description
            },
            Band = new GenerationBandDto
            {
                Id = gb.BandId,
                Name = gb.Band?.Name ?? "Unknown",
                Description = gb.Band?.Description
            }
        }).ToList();

        return new PaginatedResponse<GetGenerationBandDto>(generationBands, request.PageNumber, request.PageSize);
    }
}