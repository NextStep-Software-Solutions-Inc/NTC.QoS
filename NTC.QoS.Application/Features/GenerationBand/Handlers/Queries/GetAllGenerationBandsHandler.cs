using MediatR;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Queries;

public class GetAllGenerationBandsHandler(IGenerationBandRepository repository) : IRequestHandler<GetAllGenerationBandsRequest, PaginatedResponse<GenerationBandDto>>
{
    private readonly IGenerationBandRepository _repository = repository;

    public async Task<PaginatedResponse<GenerationBandDto>> Handle(GetAllGenerationBandsRequest request, CancellationToken cancellationToken)
    {
        var generationBandsQuery = _repository.Query();

        if (request.GenerationId.HasValue)
        {
            generationBandsQuery = generationBandsQuery.Where(gb => gb.GenerationId == request.GenerationId.Value);
        }

        if (request.BandId.HasValue)
        {
            generationBandsQuery = generationBandsQuery.Where(gb => gb.BandId == request.BandId.Value);
        }

        var generationBandDtosQuery = generationBandsQuery.Select(gb => new GenerationBandDto
        {
            Id = gb.Id,
            Generation = new ()
            {
                Id = gb.GenerationId,
                Name = gb.Generation.Name,
                Description = gb.Generation.Description
            },
            Band = new ()
            {
                Id = gb.BandId,
                Name = gb.Band.Name,
                Description = gb.Band.Description
            }
        });

        return await _repository.PaginateAsync(generationBandDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}