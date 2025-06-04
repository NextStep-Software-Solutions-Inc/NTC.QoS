using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands;

public class DeleteGenerationByIdHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<DeleteGenerationByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteGenerationByIdRequest request,
        CancellationToken cancellationToken)
    {
        var generationQuery = _repository.Query(g => g.Id == request.Id && !g.IsDeleted);
        var generation = await _repository.FirstOrDefaultAsync(generationQuery, cancellationToken);

        if (generation == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Generation",  ["Generation  not found."] }
                },
                Message = "The generation you are trying to delete does not exist.",
            };
        }

        _repository.Delete(generation);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Generation '{generation.Name}' has been successfully deleted."
        };
    }
}