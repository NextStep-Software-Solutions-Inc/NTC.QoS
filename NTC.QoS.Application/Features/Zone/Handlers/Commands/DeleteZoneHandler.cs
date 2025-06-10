using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Zone.Handlers.Commands;

public class DeleteZoneHandler(IZoneRepository repository) : IRequestHandler<DeleteZoneByIdRequest, ApiResponse<bool>>
{
    private readonly IZoneRepository _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteZoneByIdRequest request, CancellationToken cancellationToken)
    {
        var zoneQuery = _repository.Query(z => z.Id == request.Id && !z.IsDeleted);
        var zone = await _repository.FirstOrDefaultAsync(zoneQuery, cancellationToken);

        if (zone is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "The Zone you are trying to delete does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullZone", ["Zone not found."]
                    }
                }
            };
        }
        _repository.Delete(zone);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "Zone deleted successfully."
        };
    }
}
