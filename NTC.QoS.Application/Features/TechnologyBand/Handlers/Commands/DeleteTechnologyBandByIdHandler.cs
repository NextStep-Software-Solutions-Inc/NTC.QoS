using MediatR;
using NTC.QoS.Application.Features.TechnologyBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.TechnologyBand.Handlers.Commands;

public class DeleteTechnologyBandByIdHandler(ITechnologyBandRepository repository)
    : IRequestHandler<DeleteTechnologyBandByIdRequest, ApiResponse<bool>>
{
    private readonly ITechnologyBandRepository _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteTechnologyBandByIdRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query(tb => tb.Id == request.Id);
        var technologyBand = await _repository.FirstOrDefaultAsync(query, cancellationToken);

        if (technologyBand is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Technology Band not found."
            };
        }

        _repository.Delete(technologyBand);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = "Technology Band deleted successfully."
        };
    }
}