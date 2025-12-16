using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Queries;

public class GetGenerationBandByIdHandler(IGenerationBandRepository repository) : IRequestHandler<GetGenerationBandByIdRequest, ApiResponse<GenerationBandDto>>
{
    private readonly IGenerationBandRepository _repository = repository;

    public async Task<ApiResponse<GenerationBandDto>> Handle(GetGenerationBandByIdRequest request, CancellationToken cancellationToken)
    {
        var generationBandQuery = _repository.Query(gb => gb.Id.Equals(request.Id));
        var generationBand = await _repository.FirstOrDefaultAsync(generationBandQuery, cancellationToken);

        if (generationBand is null)
        {
            return new ApiResponse<GenerationBandDto>
            {
                Succeeded = false,
                Message = "Generation band not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", [ "Generation band with the specified ID does not exist." ]  }
                },
            };
        }

        return new ApiResponse<GenerationBandDto>
        {
            Succeeded = true,
            Message = "Generation band retrieved successfully",
            Data = new GenerationBandDto
            {
                Id = generationBand.Id,
                Generation = new()
                {
                    Id = generationBand.GenerationId,
                    Name = generationBand.Generation.Name,
                    Description = generationBand.Generation?.Description,
                },
                Band = new()
                {
                    Id = generationBand.BandId,
                    Name = generationBand.Band.Name,
                    Description = generationBand.Band?.Description,
                },
            },
        };
    }
}