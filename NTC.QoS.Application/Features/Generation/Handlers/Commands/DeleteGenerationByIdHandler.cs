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
        var generation = await _repository.FindAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);

        if (generation == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Error = new[] { "Generation not found." },
                Message = "The generation you are trying to delete does not exist.",
            };
        }

        _repository.Delete(generation);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Error = Array.Empty<string>(),
            Message = $"Generation '{generation.Name}' has been successfully deleted."
        };
    }
}