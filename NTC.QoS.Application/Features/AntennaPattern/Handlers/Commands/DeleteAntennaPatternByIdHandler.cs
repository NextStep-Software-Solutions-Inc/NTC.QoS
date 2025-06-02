using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Commands;

public class DeleteAntennaPatternByIdHandler(IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<DeleteAntennaPatternByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteAntennaPatternByIdRequest request,
        CancellationToken cancellationToken)
    {
        var antennaPattern = _repository.Query(ap => ap.Id == request.Id && !ap.IsDeleted);
        var existingAntennaPattern = await _repository.FirstOrDefaultAsync(antennaPattern, cancellationToken);

        if (existingAntennaPattern is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>()
                {
                    {"Error",  ["The antenna pattern you are trying to delete does not exist."] }
                },
                Message = "The antenna pattern you are trying to delete does not exist.",
            };
        }

        _repository.Delete(existingAntennaPattern);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Antenna Pattern deleted successfully."
        };
    }
}