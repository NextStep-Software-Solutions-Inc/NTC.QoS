using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using System.Net;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands;

public class CreateGenerationHandler : IRequestHandler<CreateGenerationRequest, ApiResponse<Domain.Aggregates.Generation>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository;

    public CreateGenerationHandler(IRepository<Domain.Aggregates.Generation> repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<Domain.Aggregates.Generation>> Handle(CreateGenerationRequest request, CancellationToken cancellationToken)
    {
        var existingGeneration = await _repository.FindAsync(p => p.Name == request.Generation.Name && !p.IsDeleted, cancellationToken);

        if (existingGeneration != null)
        {
            return new ApiResponse<Domain.Aggregates.Generation>(
                "Generation already exists.",
                (int)HttpStatusCode.Conflict
            );
        }

        var newGeneration = new Domain.Aggregates.Generation
        {
            Name = request.Generation.Name,
            Description = request.Generation.Description,
        };

        try
        {
            await _repository.AddAsync(newGeneration, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<Domain.Aggregates.Generation>(
                newGeneration,
                true,
                "Generation created successfully.",
                (int)HttpStatusCode.Created
            );
        }
        catch (Exception ex)
        {
            return new ApiResponse<Domain.Aggregates.Generation>(
                new List<string> { ex.Message },
                (int)HttpStatusCode.InternalServerError
            );
        }
    }
}