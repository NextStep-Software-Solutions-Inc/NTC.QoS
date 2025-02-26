using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Handlers.Commands;

public class CreateZoneHandler(IRepository<Domain.Entities.Zone> repository) : IRequestHandler<CreateZoneRequest, ApiResponse<CreateZoneDto>>
{
    private readonly IRepository<Domain.Entities.Zone> _repository = repository;

    public async Task<ApiResponse<CreateZoneDto>> Handle(CreateZoneRequest request, CancellationToken cancellationToken)
    {
        var existingZone = await _repository.FindAsync(z => z.Name == request.Zone.Name && z.BarangayId == request.Zone.BarangayId && !z.IsDeleted, cancellationToken);

        if (existingZone != null)
        {
            return new ApiResponse<CreateZoneDto>
            {
                Message = "Zone already exists.",
                Succeeded = false
            };
        }

        var newZone = new Domain.Entities.Zone
        {
            BarangayId = request.Zone.BarangayId,
            Name = request.Zone.Name,
        };

        _repository.Add(newZone);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateZoneDto>
        {
            Data = new CreateZoneDto
            {
                BarangayId = newZone.BarangayId,
                Name = newZone.Name,
            },

            Message = "Zone created successfully.",
            Succeeded = true
        };
    }
}