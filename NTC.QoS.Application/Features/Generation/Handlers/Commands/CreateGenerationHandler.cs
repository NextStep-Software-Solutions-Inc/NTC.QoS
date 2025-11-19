using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands;

public class CreateGenerationHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<CreateGenerationRequest, ApiResponse<GenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;

    public async Task<ApiResponse<GenerationDto>> Handle(CreateGenerationRequest request, CancellationToken cancellationToken)
    {
        // Include soft-deleted records
        var generationQueryAll = _repository.QueryWithIgnoreQueryFilters(g => g.Name == request.Generation.Name);
        var existing = await _repository.FirstOrDefaultAsync(generationQueryAll, cancellationToken);

        if (existing is not null && existing.IsDeleted)
        {
            var restoreQuery = _repository.QueryWithIgnoreQueryFilters(g => g.Id == existing.Id && g.IsDeleted);
            await _repository.RestoreAsync(restoreQuery, cancellationToken);
            return new ApiResponse<GenerationDto>
            {
                Data = new GenerationDto
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    Description = existing.Description
                },
                Succeeded = true,
                Message = "Generation restored successfully."
            };
        }

        if (existing is not null)
        {
            return new ApiResponse<GenerationDto>
            {
                Succeeded = false,
                Message = "Generation already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Duplicate", ["Generation with the same name already exists."]}
                }
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