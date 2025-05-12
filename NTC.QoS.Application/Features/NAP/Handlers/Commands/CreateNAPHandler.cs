using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

public class CreateNAPHandler(INAPRepository repository) : IRequestHandler<CreateNAPRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _repository = repository;

    public async Task<ApiResponse<NAPDto>> Handle(CreateNAPRequest request, CancellationToken cancellationToken)
    {
        Point location = GeometryHelper.CreatePoint(x: request.NAP.Latitude, y: request.NAP.Longitude);

        var NAP = new Domain.Aggregates.NAP
        {
            ServiceProviderId = request.NAP.ServiceProviderId,
            Location = location,
            Address = request.NAP.Address,
            IsFileUploaded = request.NAP.IsFileUploaded,
        };

        _repository.Add(NAP);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<NAPDto>
        {
            Data = new NAPDto
            {
                Id = NAP.Id,
                ServiceProvider = new ()
                {
                    Id = NAP.ServiceProviderId,
                    Service = new ()
                    {
                        Id = NAP.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = NAP.ServiceProvider?.Service?.Type.ToString() ?? string.Empty,
                    },
                    Provider = new ()
                    {
                        Id = NAP.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Color = NAP.ServiceProvider?.Provider?.Color ?? string.Empty,
                    }
                },
                Address = NAP.Address,
                IsFileUploaded = NAP.IsFileUploaded,
                Latitude = NAP.Location.X,
                Longitude = NAP.Location.Y,
                Distance = 0
            },
            Message = "NAP created successfully.",
            Succeeded = true
        };
    }
}
