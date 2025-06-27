using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Queries;

public class GetGenerationBandByIdHandler(IGenerationBandRepository repository) : IRequestHandler<GetGenerationBandByIdRequest, ApiResponse<GetGenerationBandDto>>
{
    private readonly IGenerationBandRepository _repository = repository;

    public async Task<ApiResponse<GetGenerationBandDto>> Handle(GetGenerationBandByIdRequest request, CancellationToken cancellationToken)
    {
        var generationBandQuery = _repository.Query(gb => !gb.IsDeleted && gb.Id.Equals(request.Id));
        var generationBand = await _repository.FirstOrDefaultAsync(generationBandQuery, cancellationToken);

        if (generationBand is null)
        {
            return new ApiResponse<GetGenerationBandDto>
            {
                Succeeded = false,
                Message = "Generation band not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", [ "Generation band with the specified ID does not exist." ]  }
                },
            };
        }

        return new ApiResponse<GetGenerationBandDto>
        {
            Succeeded = true,
            Message = "Generation band retrieved successfully",
            Data = new GetGenerationBandDto
            {
                Id = generationBand.Id,
                Generation = new GenerationBandDto
                {
                    Id = generationBand.GenerationId,
                    Name = generationBand.Generation?.Name,
                    Description = generationBand.Generation?.Description,
                },
                Band = new GenerationBandDto
                {
                    Id = generationBand.BandId,
                    Name = generationBand.Band?.Name,
                    Description = generationBand.Band?.Description,
                },
            },
        };
    }
}