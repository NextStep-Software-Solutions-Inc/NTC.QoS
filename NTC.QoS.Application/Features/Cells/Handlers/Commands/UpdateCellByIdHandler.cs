using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class UpdateCellByIdHandler(
    ICellRepository cellRepository,
    IServiceProviderRepository serviceProviderRepository,
    IConnectivityAssessmentTriggerService connectivityAssessmentTriggerService,
    ILogger<UpdateCellByIdHandler> logger) : IRequestHandler<UpdateCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IConnectivityAssessmentTriggerService _connectivityAssessmentTriggerService = connectivityAssessmentTriggerService;
    private readonly ILogger<UpdateCellByIdHandler> _logger = logger;

    public async Task<ApiResponse<CellDto>> Handle(UpdateCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query(c => c.Id.Equals(request.Id));
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell not found",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCell", ["Cell not found."]
                    }
                }
            };
        }

        // Defensive check: Ensure Location exists (protect against corrupt data)
        if (cell.Location == null)
        {
            _logger.LogError("Cell {CellId} has null Location - possible data corruption", cell.Id);
            return new ApiResponse<CellDto>
            {
                Message = "Cell data is corrupted (missing location).",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "DataIntegrity", ["The cell location data is missing or corrupted."] }
                }
            };
        }

        if (request.Cell.Latitude.HasValue != request.Cell.Longitude.HasValue)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Invalid location update.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Location", ["Both latitude and longitude must be provided together."] }
                }
            };
        }

        var updatedServiceProviderId = request.Cell.ServiceProviderId ?? cell.ServiceProviderId;
        if (updatedServiceProviderId != cell.ServiceProviderId)
        {
            var serviceProviderExists = await _serviceProviderRepository.FirstOrDefaultAsync(
                _serviceProviderRepository.QueryAsNoTracking(sp => sp.Id == updatedServiceProviderId && sp.Service.Type == ServiceTypeEnum.Wireless)
                    .Select(sp => sp.Id),
                cancellationToken);

            if (serviceProviderExists == Guid.Empty)
            {
                return new ApiResponse<CellDto>
                {
                    Message = "Service Provider not found.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "NullServiceProvider", ["Service Provider not found or is not wireless."] }
                    }
                };
            }
        }

        var updatedTowerType = cell.TowerType;
        if (request.Cell.TowerType is not null)
        {
            updatedTowerType = Enum.TryParse<TowerTypeEnum>(request.Cell.TowerType, true, out var towerType)
                ? towerType
                : TowerTypeEnum.Unknown;
        }

        var updatedLocation = request.Cell.Latitude.HasValue && request.Cell.Longitude.HasValue
            ? GeometryHelper.CreatePoint(new Coordinate(y: request.Cell.Latitude.Value, x: request.Cell.Longitude.Value))
            : cell.Location;

        const double duplicateDistanceTolerance = 0.000001d;
        var duplicateCellId = await _cellRepository.FirstOrDefaultAsync(
            _cellRepository.Query(c =>
                    c.Id != cell.Id
                    && c.ServiceProviderId == updatedServiceProviderId
                    && c.TowerType == updatedTowerType
                    && c.Location.Distance(updatedLocation) <= duplicateDistanceTolerance)
                .Select(c => c.Id),
            cancellationToken);

        if (duplicateCellId != Guid.Empty)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["A different cell with the same service provider, tower type, and location already exists."]
                    }
                }
            };
        }

        cell.ServiceProviderId = updatedServiceProviderId;
        cell.TowerType = updatedTowerType;
        cell.Location = updatedLocation;

        var updatedAddress = request.Cell.Address ?? cell.Address;

        if (request.Cell.Group is not null) cell.Group = request.Cell.Group;
        if (request.Cell.Description is not null) cell.Description = request.Cell.Description;
        if (request.Cell.Address is not null) cell.Address = request.Cell.Address;
        if (request.Cell.CID is not null) cell.CID = request.Cell.CID;
        if (request.Cell.LAC is not null) cell.LAC = request.Cell.LAC;
        if (request.Cell.TAC is not null) cell.TAC = request.Cell.TAC;
        if (request.Cell.SiteId is not null) cell.SiteId = request.Cell.SiteId;
        if (request.Cell.Elevation.HasValue) cell.Elevation = request.Cell.Elevation.Value;
        if (request.Cell.IsVerified.HasValue) cell.IsVerified = request.Cell.IsVerified.Value;
        if (!string.IsNullOrWhiteSpace(request.Cell.Status))
        {
            if (!Enum.TryParse<CellStatusEnum>(request.Cell.Status, true, out var status))
            {
                return new ApiResponse<CellDto>
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Status", ["Status is not a valid CellStatusEnum value."] }
                    }
                };
            }

            cell.Status = status;
        }

        var hasIdentityFieldChange =
            request.Cell.ServiceProviderId.HasValue ||
            request.Cell.Address is not null ||
            request.Cell.TowerType is not null ||
            (request.Cell.Latitude.HasValue && request.Cell.Longitude.HasValue);

        if (request.Cell.Name is not null)
        {
            cell.Name = request.Cell.Name;
        }
        else if (hasIdentityFieldChange)
        {
            var serviceProviderName = await _serviceProviderRepository.FirstOrDefaultAsync(
                _serviceProviderRepository.QueryAsNoTracking(sp => sp.Id == cell.ServiceProviderId)
                    .Select(sp => sp.Provider.Name),
                cancellationToken) ?? string.Empty;

            cell.Name = $"Cell-{serviceProviderName}-{updatedAddress ?? string.Empty}-{cell.TowerType}-{cell.Location.Coordinate.X}-{cell.Location.Coordinate.Y}";
        }

        var duplicateNameId = await _cellRepository.FirstOrDefaultAsync(
            _cellRepository.Query(c => c.Id != cell.Id && c.Name.ToLower() == cell.Name.ToLower())
                .Select(c => c.Id),
            cancellationToken);

        if (duplicateNameId != Guid.Empty)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["A different cell with the same name already exists."]
                    }
                }
            };
        }

        _cellRepository.Update(cell);
        await _cellRepository.SaveChangesAsync(cancellationToken);

        // Trigger connectivity assessment generation after successful cell update
        // Location is guaranteed non-null due to earlier check
        await _connectivityAssessmentTriggerService.TriggerAssessmentAsync(
            cell.Location,
            cell.Id,
            "Cell",
            request.RegionId,
            cancellationToken);

        var updatedCell = await _cellRepository.FirstOrDefaultAsync(
            _cellRepository.QueryAsNoTracking(c => c.Id == cell.Id)
                .Select(c => new CellDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Group = c.Group,
                    Description = c.Description ?? string.Empty,
                    Address = c.Address ?? string.Empty,
                    ServiceProvider = new()
                    {
                        Id = c.ServiceProviderId,
                        Service = new()
                        {
                            Id = c.ServiceProvider != null && c.ServiceProvider.Service != null ? c.ServiceProvider.Service.Id : Guid.Empty,
                            Name = c.ServiceProvider != null && c.ServiceProvider.Service != null ? c.ServiceProvider.Service.Name : string.Empty,
                            Type = c.ServiceProvider != null && c.ServiceProvider.Service != null ? c.ServiceProvider.Service.Type.ToString() : string.Empty
                        },
                        Provider = new()
                        {
                            Id = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Id : Guid.Empty,
                            Name = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Name : string.Empty,
                            Description = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Description : null,
                            LicenseNumber = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.LicenseNumber : null,
                            Color = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Color : string.Empty
                        }
                    },
                    CID = c.CID,
                    LAC = c.LAC,
                    TAC = c.TAC,
                    SiteId = c.SiteId,
                    TowerType = c.TowerType.ToString(),
                    Elevation = c.Elevation,
                    Latitude = c.Location.Coordinate.Y,
                    Longitude = c.Location.Coordinate.X,
                    IsVerified = c.IsVerified,
                    IsFileUploaded = c.IsFileUploaded,
                    Status = EnumHelper<CellStatusEnum>.GetName(c.Status)
                }),
            cancellationToken);

        return new ApiResponse<CellDto>
        {
            Data = updatedCell!,
            Succeeded = true,
            Message = "Cell updated successfully."
        };
    }
}
