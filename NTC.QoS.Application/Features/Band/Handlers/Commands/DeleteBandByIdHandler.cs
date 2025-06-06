using MediatR;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Band.Handlers.Commands;

public class DeleteBandByIdHandler(IRepository<Domain.Entities.Band> repository) : IRequestHandler<DeleteBandByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Band> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteBandByIdRequest request,
        CancellationToken cancellationToken)
    {
        var bandQuery = _repository.Query(b => b.Id == request.Id && !b.IsDeleted);
        var band = await _repository.FirstOrDefaultAsync(bandQuery, cancellationToken);

        if (band == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Band", new[] { "Band not found." } }
                },
                Message = "The band you are trying to delete does not exist.",
            };
        }

        _repository.Delete(band);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Band '{band.Name}' has been successfully deleted."
        };
    }
}
