using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;

public class CreateCellularCoverageHandler(ICellularCoverageRepository cellularCoverageRepository) : IRequestHandler<CreateCellularCoverageRequest, ApiResponse<CellularCoverageDto>>
{
    private readonly ICellularCoverageRepository _cellularCoverageRepository = cellularCoverageRepository;

    public async Task<ApiResponse<CellularCoverageDto>> Handle(CreateCellularCoverageRequest request, CancellationToken cancellationToken)
    {
        Point location = GeometryHelper.CreatePoint(new Coordinate(y: request.CellularCoverage.Latitude, x: request.CellularCoverage.Longitude));

        var newCellularCoverage = new Domain.Entities.CellularCoverage
        {
            NetworkId = request.CellularCoverage.NetworkId,
            Location = location,
            Strength = request.CellularCoverage.Strength,
            Address = request.CellularCoverage.Address,
            IsFileUploaded = false
        };

        _cellularCoverageRepository.Add(newCellularCoverage);
        await _cellularCoverageRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CellularCoverageDto>
        {
            Data = new CellularCoverageDto
            {
                Id = newCellularCoverage.Id,
                NetworkId = newCellularCoverage.NetworkId,
                Lat = newCellularCoverage.Location.Coordinate.Y,
                Lng = newCellularCoverage.Location.Coordinate.X,
                Strength = newCellularCoverage.Strength,
                Band = newCellularCoverage.Network?.UpLink?.Band?.Name ?? string.Empty,
                Gen = newCellularCoverage.Network?.SystemType?.Technology?.Generation?.Name ?? string.Empty,
                Std = newCellularCoverage.Network?.SystemType?.Technology?.Standard ?? string.Empty,
                Provider = newCellularCoverage.Network?.UpLink?.ServiceProvider?.Provider != null ? new CCProviderDto
                {
                    Name = newCellularCoverage.Network.UpLink.ServiceProvider.Provider.Name,
                    Color = newCellularCoverage.Network.UpLink.ServiceProvider.Provider.Color
                } : null
            },
            Message = "Cellular coverage created successfully.",
            Succeeded = true
        };
    }
}
