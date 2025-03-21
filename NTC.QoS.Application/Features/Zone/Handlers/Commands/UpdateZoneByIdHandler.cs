using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Zone.Handlers.Commands;

public class UpdateZoneByIdHandler(IRepository<Domain.Entities.Zone> repository) : IRequestHandler<UpdateZoneByIdRequest, ApiResponse<Domain.Entities.Zone>>
{
    private readonly IRepository<Domain.Entities.Zone> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.Zone>> Handle(UpdateZoneByIdRequest request, CancellationToken cancellationToken)
    {
        var zone = await _repository.FindAsync(z => z.Id == request.Id && !z.IsDeleted, cancellationToken);
        if (zone == null)
        {
            return new ApiResponse<Domain.Entities.Zone>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Zone not found." },
                Message = "The zone you are trying to edit does not exist."
            };
        }
        zone.Name = request.Zone.Name;

        _repository.Update(zone);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<Domain.Entities.Zone>
        {
            Succeeded = true,
            Data = zone,
            Message = "Request successful! Your changes have been saved."
        };
    }
}