using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class CreateCellHandler(ICellRepository cellRepository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateCellRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<CellDto>> Handle(CreateCellRequest request, CancellationToken cancellationToken)
    {
        Point location = GeometryHelper.CreatePoint(new Coordinate(y: request.Cell.Latitude, x: request.Cell.Longitude));

        var cellQuery = _cellRepository.Query(c => c.Location.Equals(location) && c.Name == request.Cell.Name);
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is not null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Cell with the same name and location already exists."]
                    }
                }
            };
        }

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Cell.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Service Provider not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullServiceProvider", ["Service Provider not found."]
                    }
                }
            };
        }

        var newCell = new Domain.Aggregates.Cell
        {
            Name = request.Cell.Name,
            Group = request.Cell.Group,
            Description = request.Cell.Description,
            Address = request.Cell.Address,
            ServiceProviderId = serviceProvider.Id,
            CID = request.Cell.CID,
            SiteId = request.Cell.SiteId,
            TowerType = request.Cell.TowerType,
            Elevation = request.Cell.Elevation,
            TAC = request.Cell.TAC,
            LAC = request.Cell.LAC,
            Location = location,
            Status = EnumHelper<CellStatusEnum>.GetValue(request.Cell.Status),
            IsFileUploaded = false
        };

        _cellRepository.Add(newCell);
        await _cellRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CellDto>
        {
            Data = new CellDto
            {
                Id = newCell.Id,
                Name = newCell.Name,
                Group = newCell.Group,
                Description = newCell.Description ?? string.Empty,
                Address = newCell.Address ?? string.Empty,
                ServiceProvider = new()
                {
                    Id = newCell!.ServiceProviderId,
                    Service = new()
                    {
                        Id = newCell.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = newCell.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = newCell.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new()
                    {
                        Id = newCell.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = newCell.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = newCell.ServiceProvider?.Provider?.Description,
                        LicenseNumber = newCell.ServiceProvider?.Provider?.LicenseNumber,
                        Color = newCell.ServiceProvider?.Provider?.Color ?? string.Empty
                    }
                },
                CID = newCell.CID,
                LAC = newCell.LAC,
                TAC = newCell.TAC,
                SiteId = newCell.SiteId,
                TowerType = newCell.TowerType,
                Elevation = newCell.Elevation,
                Latitude = newCell.Location.Coordinate.Y,
                Longitude = newCell.Location.Coordinate.X,
                IsVerified = newCell.IsVerified,
                IsFileUploaded = newCell.IsFileUploaded,
                Status = EnumHelper<CellStatusEnum>.GetName(newCell.Status)
            },
            Message = "Cell created successfully.",
            Succeeded = true
        };
    }
}
