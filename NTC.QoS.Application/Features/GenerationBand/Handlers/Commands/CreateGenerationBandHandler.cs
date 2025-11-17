using MediatR;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Commands;

public class CreateGenerationBandHandler(IRepository<Domain.Aggregates.GenerationBand> repository, IBandRepository bandRepository, IGenerationRepository generationRepository) : IRequestHandler<CreateGenerationBandRequest, ApiResponse<GenerationBandDto>>
{
    private readonly IRepository<Domain.Aggregates.GenerationBand> _repository = repository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly IGenerationRepository _generationRepository = generationRepository;

    public async Task<ApiResponse<GenerationBandDto>> Handle(CreateGenerationBandRequest request, CancellationToken cancellationToken)
    {
        var existingGenerationBandQuery = _repository.Query(gb => gb.GenerationId == request.GenerationBand.GenerationId && gb.BandId == request.GenerationBand.BandId);
        var existingGenerationBand = await _repository.FirstOrDefaultAsync(existingGenerationBandQuery, cancellationToken);

        if (existingGenerationBand is not null)
        {
            return new ApiResponse<GenerationBandDto>
            {
                Message = "Generation band already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error", ["Generation band with the same GenerationId and BandId already exists."] }
                },
            };
        }

        var bandQuery = _bandRepository.Query(b => b.Id == request.GenerationBand.BandId);
        var band = await _bandRepository.FirstOrDefaultAsync(bandQuery, cancellationToken);

        if (band is null)
        {
            return new ApiResponse<GenerationBandDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error",  ["Band not found."] }
                },
                Message = "Band not found.",
            };
        }

        var generationQuery = _generationRepository.Query(g => g.Id == request.GenerationBand.GenerationId);
        var generation = await _generationRepository.FirstOrDefaultAsync(generationQuery, cancellationToken);

        if (generation is null)
        {
            return new ApiResponse<GenerationBandDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error",  ["Generation not found."] }
                },
                Message = "Generation not found.",
            };
        }

        var newGenerationBand = new Domain.Aggregates.GenerationBand
        {
            GenerationId = generation.Id,
            BandId = band.Id
        };
        _repository.Add(newGenerationBand);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<GenerationBandDto>
        {
            Data = new GenerationBandDto
            {
                Id = newGenerationBand.Id,
                Generation = new()
                {
                    Id = newGenerationBand.Generation.Id,
                    Name = newGenerationBand.Generation.Name,
                    Description = newGenerationBand.Generation.Description
                },
                Band = new()
                {
                    Id = newGenerationBand.Band.Id,
                    Name = newGenerationBand.Band.Name,
                    Description = newGenerationBand.Band.Description
                }
            },
            Message = "Generation band created successfully.",
            Succeeded = true
        };
    }
}
