using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands;

public class CreateGenerationHandler (IRepository<Domain.Aggregates.Generation> repository): IRequestHandler<CreateGenerationRequest, ApiResponse<GenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;

    public async Task<ApiResponse<GenerationDto>> Handle(CreateGenerationRequest request, CancellationToken cancellationToken)
    {
        var generationQuery = _repository.Query(g => g.Name == request.Generation.Name);
        var generation = await _repository.FirstOrDefaultAsync(generationQuery, cancellationToken);

        if (generation is not null)
        {
            return new ApiResponse<GenerationDto>
            {
                Succeeded = false,
                Message = "Generation already exists."
            };
        }
                
        var newGeneration = new Domain.Aggregates.Generation
        {
            Name = request.Generation.Name,
            Description = request.Generation.Description,
        };

        _repository.Add(newGeneration);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<GenerationDto>
        {
            Data = new GenerationDto
            {
                Id = newGeneration.Id,
                Name = newGeneration.Name,
                Description = newGeneration.Description,
            },
            Succeeded = true,
            Message = "Generation created successfully.",
        };
    }
}