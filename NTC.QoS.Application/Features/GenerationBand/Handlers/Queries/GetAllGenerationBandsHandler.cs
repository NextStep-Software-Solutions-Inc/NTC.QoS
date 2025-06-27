using MediatR;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Queries;

public class GetAllGenerationBandsHandler(IGenerationBandRepository repository) : IRequestHandler<GetAllGenerationBandsRequest, PaginatedResponse<GetGenerationBandDto>>
{
    private readonly IGenerationBandRepository _repository = repository;

    public async Task<PaginatedResponse<GetGenerationBandDto>> Handle(GetAllGenerationBandsRequest request, CancellationToken cancellationToken)
    {
        var generationBandsQuery = _repository.Query(gb => !gb.IsDeleted);

        if (request.GenerationId.HasValue)
        {
            generationBandsQuery = generationBandsQuery.Where(gb => gb.GenerationId == request.GenerationId.Value);
        }

        if (request.BandId.HasValue)
        {
            generationBandsQuery = generationBandsQuery.Where(gb => gb.BandId == request.BandId.Value);
        }

        var generationBandDtosQuery = generationBandsQuery.Select(gb => new GetGenerationBandDto
        {
            Id = gb.Id,
            Generation = new GenerationBandDto
            {
                Id = gb.GenerationId,
                Name = gb.Generation != null ? gb.Generation.Name : "Unknown",
                Description = gb.Generation != null ? gb.Generation.Description : null
            },
            Band = new GenerationBandDto
            {
                Id = gb.BandId,
                Name = gb.Band != null ? gb.Band.Name : "Unknown",
                Description = gb.Band != null ? gb.Band.Description : null
            }
        });

        return await _repository.PaginateAsync(generationBandDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}