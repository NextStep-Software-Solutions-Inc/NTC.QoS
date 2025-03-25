using MediatR;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Commands;

public class CreateGenerationBandHandler(IRepository<Domain.Aggregates.GenerationBand> repository) : IRequestHandler<CreateGenerationBandRequest, ApiResponse<CreateGenerationBandDto>>
{
    private readonly IRepository<Domain.Aggregates.GenerationBand> _repository = repository;

    public async Task<ApiResponse<CreateGenerationBandDto>> Handle(CreateGenerationBandRequest request, CancellationToken cancellationToken)
    {
        var existingGenerationBand = await _repository.FindAsync(gb => gb.GenerationId == request.GenerationBand.GenerationId && gb.BandId == request.GenerationBand.BandId, cancellationToken);

        if (existingGenerationBand is not null)
        {
            return new ApiResponse<CreateGenerationBandDto>
            {
                Message = "Generation band already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error", ["Generation band with the same GenerationId and BandId already exists."] }
                },
            };
        }

        var newGenerationBand = new Domain.Aggregates.GenerationBand
        {
            GenerationId = request.GenerationBand.GenerationId,
            BandId = request.GenerationBand.BandId
        };
        _repository.Add(newGenerationBand);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateGenerationBandDto>
        {
            Data = new CreateGenerationBandDto
            {
                GenerationId = newGenerationBand.GenerationId,
                BandId = newGenerationBand.BandId
            },
            Message = "Generation band created successfully.",
            Succeeded = true
        };
    }
}
