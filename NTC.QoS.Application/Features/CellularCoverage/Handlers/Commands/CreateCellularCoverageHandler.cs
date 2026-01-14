using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;

public class CreateCellularCoverageHandler(
    ICellularCoverageRepository cellularCoverageRepository,
    IRepository<Domain.Aggregates.Network> networkRepository) : IRequestHandler<CreateCellularCoverageRequest, ApiResponse<CellularCoverageDto>>
{
    private readonly ICellularCoverageRepository _cellularCoverageRepository = cellularCoverageRepository;
    private readonly IRepository<Domain.Aggregates.Network> _networkRepository = networkRepository;

    public async Task<ApiResponse<CellularCoverageDto>> Handle(CreateCellularCoverageRequest request, CancellationToken cancellationToken)
    {
        Point location = GeometryHelper.CreatePoint(new Coordinate(y: request.CellularCoverage.Latitude, x: request.CellularCoverage.Longitude));

        var networkQuery = _networkRepository.Query(p => p.Id == request.CellularCoverage.NetworkId);
        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<CellularCoverageDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullNetwork", ["Network not found."]
                    }
                },
                Message = "Network not found."
            };
        }

        var newCellularCoverage = new Domain.Entities.CellularCoverage
        {
            NetworkId = network.Id,
            Location = location,
            SignalStrength = request.CellularCoverage.SignalStrength,
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
                SignalStrength = newCellularCoverage.SignalStrength,
                Gen = newCellularCoverage.Network?.SystemType?.Technology?.Generation?.Name ?? string.Empty,
                Std = newCellularCoverage.Network?.SystemType?.Technology?.Standard ?? string.Empty,
            },
            Message = "Cellular coverage created successfully.",
            Succeeded = true
        };
    }
}
