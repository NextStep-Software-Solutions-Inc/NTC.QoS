using MediatR;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Queries;

public class GetCellularCoverageByIdHandler(ICellularCoverageRepository cellularCoverageRepository) : IRequestHandler<GetCellularCoverageByIdRequest, ApiResponse<CellularCoverageDto>>
{
    private readonly ICellularCoverageRepository _cellularCoverageRepository = cellularCoverageRepository;

    public async Task<ApiResponse<CellularCoverageDto>> Handle(GetCellularCoverageByIdRequest request, CancellationToken cancellationToken)
    {
        var cellularCoverageQuery = _cellularCoverageRepository.Query(cc => cc.Id.Equals(request.Id));
        var cellularCoverage = await _cellularCoverageRepository.FirstOrDefaultAsync(cellularCoverageQuery, cancellationToken);
        if (cellularCoverage is null)
        {
            return new ApiResponse<CellularCoverageDto>
            {
                Succeeded = false,
                Message = "Cellular coverage record not found.",
            };
        }

        return new ApiResponse<CellularCoverageDto>
        {
            Succeeded = true,
            Message = "Cellular coverage record retrieved successfully.",
            Data = new ()
            {
                Id = cellularCoverage.Id,
                NetworkId = cellularCoverage.NetworkId,
                Lat = cellularCoverage.Location.Coordinate.Y,
                Lng = cellularCoverage.Location.Coordinate.X,
                Strength = cellularCoverage.Strength,
                Band = cellularCoverage.Network.UpLink != null ? cellularCoverage.Network.UpLink.Band.Name : string.Empty,
                Gen = cellularCoverage.Network.SystemType != null && cellularCoverage.Network.SystemType.Technology != null ? cellularCoverage.Network.SystemType.Technology.Generation.Name : string.Empty,
                Std = cellularCoverage.Network.SystemType != null && cellularCoverage.Network.SystemType.Technology != null ? cellularCoverage.Network.SystemType.Technology.Standard : string.Empty,
                Provider = cellularCoverage.Network.UpLink != null ? new()
                {
                    Name = cellularCoverage.Network.UpLink.ServiceProvider.Provider.Name,
                    Color = cellularCoverage.Network.UpLink.ServiceProvider.Provider.Color
                } : null
            }
        };
    }
}
