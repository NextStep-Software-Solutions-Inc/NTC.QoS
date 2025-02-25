using MediatR;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Commands;

public class DeleteAntennaPatternByIdHandler(IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<DeleteAntennaPatternByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteAntennaPatternByIdRequest request,
        CancellationToken cancellationToken)
    {
        var antennaPattern = await _repository.FindAsync(ap => ap.Id == request.Id && !ap.IsDeleted, cancellationToken);

        if (antennaPattern == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Error = new[] { "Antenna pattern not found." },
                Message = "The antenna pattern you are trying to delete does not exist.",
            };
        }

        _repository.Delete(antennaPattern);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Error = Array.Empty<string>(),
            Message = $"Antenna pattern '{antennaPattern.Name}' has been successfully deleted."
        };
    }
}