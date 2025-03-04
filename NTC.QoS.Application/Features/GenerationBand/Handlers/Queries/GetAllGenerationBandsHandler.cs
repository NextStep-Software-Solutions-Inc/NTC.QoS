using MediatR;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Queries;

public class GetAllGenerationBandsHandler(IGenerationBandRepository repository) : IRequestHandler<GetAllGenerationBandsRequest, PaginatedResponse<GetGenerationBandDto>>
{
    private readonly IGenerationBandRepository _repository = repository;
    public async Task<PaginatedResponse<GetGenerationBandDto>> Handle(GetAllGenerationBandsRequest request, CancellationToken cancellationToken)
    {
        var allGenerationBands = await _repository.ListAsync(gb => !gb.IsDeleted, cancellationToken);

        if (request.GenerationId.HasValue)
        {
            allGenerationBands = allGenerationBands.Where(gb => gb.GenerationId == request.GenerationId.Value).ToList();
        }

        if (request.BandId.HasValue)
        {
            allGenerationBands = allGenerationBands.Where(gb => gb.BandId == request.BandId.Value).ToList();
        }

        var generationBandDtos = allGenerationBands.Select(gb => new GetGenerationBandDto
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

        return new PaginatedResponse<GetGenerationBandDto>(generationBandDtos.ToList(), request.PageNumber, request.PageSize);
    }
}