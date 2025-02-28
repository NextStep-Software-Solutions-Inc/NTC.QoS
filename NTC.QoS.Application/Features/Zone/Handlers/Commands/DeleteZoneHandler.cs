using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Zone.Handlers.Commands;

public class DeleteZoneHandler(IRepository<Domain.Entities.Zone> repository) : IRequestHandler<DeleteZoneByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Zone> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteZoneByIdRequest request, CancellationToken cancellationToken)
    {
        var zone = await _repository.FindAsync(z => z.Id == request.Id && !z.IsDeleted, cancellationToken);
        if (zone == null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "Zone not found."
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
