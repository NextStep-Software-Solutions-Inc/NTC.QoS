using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands;

public class CreateGenerationHandler (IRepository<Domain.Aggregates.Generation> repository): IRequestHandler<CreateGenerationRequest, ApiResponse<CreateGenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;

    public async Task<ApiResponse<CreateGenerationDto>> Handle(CreateGenerationRequest request, CancellationToken cancellationToken)
    {
        var existingGeneration = await _repository.FindAsync(p => p.Name == request.Generation.Name && !p.IsDeleted, cancellationToken);

        if (existingGeneration != null)
        {
            return new ApiResponse<CreateGenerationDto>(
                "Generation already exists."
            );
        }

        var newGeneration = new Domain.Aggregates.Generation
        {
            Name = request.Generation.Name,
            Description = request.Generation.Description,
        };

        _repository.Add(newGeneration);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateGenerationDto>
        {
            Data = new CreateGenerationDto
            {
                Name = newGeneration.Name,
                Description = newGeneration.Description,
            },
            Succeeded = true,
            Message = "Generation created successfully.",
        };
    }
}