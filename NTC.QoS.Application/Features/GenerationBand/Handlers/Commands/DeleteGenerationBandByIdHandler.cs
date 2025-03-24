using MediatR;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.GenerationBand.Handlers.Commands;

public class DeleteGenerationBandByIdHandler(IRepository<Domain.Aggregates.GenerationBand> repository) : IRequestHandler<DeleteGenerationBandByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.GenerationBand> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteGenerationBandByIdRequest request,
        CancellationToken cancellationToken)
    {
        var generationBand = await _repository.FindAsync(gb => gb.Id == request.Id && !gb.IsDeleted, cancellationToken);

        if (generationBand is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "GenerationBand",  ["Generation band not found."] }
                },
                Message = "The generation band you are trying to delete does not exist.",
            };
        }

        _repository.Delete(generationBand);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Generation band has been successfully deleted."
        };
    }
}