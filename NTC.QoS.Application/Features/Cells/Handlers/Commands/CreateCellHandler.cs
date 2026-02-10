using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class CreateCellHandler(ICellRepository cellRepository, IServiceProviderRepository serviceProviderRepository, IValidator<CreateCellRequest> validator) : IRequestHandler<CreateCellRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IValidator<CreateCellRequest> _validator = validator;

    public async Task<ApiResponse<CellDto>> Handle(CreateCellRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Request is null.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Request", new[] { "Request cannot be null." } }
                }
            };
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return new ApiResponse<CellDto>
            {
                Message = "Validation failed.",
                Succeeded = false,
                Errors = errors
            };
        }

        // inputs normalized below

        // proximity tolerance in degrees (~1e-5 is ~1m at equator; adjust as appropriate)
        const double eps = 0.00001d;

        // normalize inputs and create geometry
        var nameNormalized = request.Cell.Name!.Trim().ToLowerInvariant();
        var lat = request.Cell.Latitude;
        var lon = request.Cell.Longitude;
        Point location = GeometryHelper.CreatePoint(new Coordinate(y: lat, x: lon));

        // Use spatial distance-based duplicate detection. This relies on repository/EF translating
        // `Location.Distance(location)` to a database spatial function. `eps` is in the same units
        // as the stored geometry (degrees for SRID 4326) — adjust if using geography/meters.
        var cellQuery = _cellRepository.Query(c => c.Location != null
                  && c.Name != null
                  && c.Name.ToLower() == nameNormalized
                  && c.Location.Distance(location) <= eps);

        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is not null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", new[] { "Cell with the same (or very close) name and location already exists." } }
                }
            };
        }

        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(
            _serviceProviderRepository.Query(sp => sp.Id == request.Cell.ServiceProviderId),
            cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Service Provider not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullServiceProvider", new[] { "Service Provider not found." } }
                }
            };
        }

        var newCell = new Domain.Aggregates.Cell
        {
            Name = request.Cell.Name!,
            Group = request.Cell.Group ?? string.Empty,
            Description = request.Cell.Description,
            Address = request.Cell.Address,
            ServiceProviderId = serviceProvider.Id,
            CID = request.Cell.CID ?? string.Empty,
            SiteId = request.Cell.SiteId ?? string.Empty,
            TowerType = !string.IsNullOrEmpty(request.Cell.TowerType) && Enum.TryParse<TowerTypeEnum>(request.Cell.TowerType, true, out var towerType) ? towerType : TowerTypeEnum.Unknown,
            Elevation = request.Cell.Elevation,
            TAC = request.Cell.TAC ?? string.Empty,
            LAC = request.Cell.LAC,
            Location = location,
            Status = EnumHelper<CellStatusEnum>.GetValue(request.Cell.Status),
            IsFileUploaded = false,
            IsVerified = request.Cell.IsVerified
        };

        _cellRepository.Add(newCell);
        try
        {
            await _cellRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Avoid referencing EF types in the application layer; map exceptions generically
            return new ApiResponse<CellDto>
            {
                Message = "Error while creating cell.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { ex.GetType().Name, new[] { ex.Message } }
                }
            };
        }

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
                    Id = serviceProvider.Id,
                    Service = new()
                    {
                        Id = serviceProvider.Service?.Id ?? Guid.Empty,
                        Name = serviceProvider.Service?.Name ?? string.Empty,
                        Type = serviceProvider.Service is not null ? serviceProvider.Service.Type.ToString() : string.Empty
                    },
                    Provider = new()
                    {
                        Id = serviceProvider.Provider?.Id ?? Guid.Empty,
                        Name = serviceProvider.Provider?.Name ?? string.Empty,
                        Description = serviceProvider.Provider?.Description,
                        LicenseNumber = serviceProvider.Provider?.LicenseNumber,
                        Color = serviceProvider.Provider?.Color ?? string.Empty
                    }
                },
                CID = newCell.CID,
                LAC = newCell.LAC,
                TAC = newCell.TAC,
                SiteId = newCell.SiteId,
                TowerType = newCell.TowerType.ToString(),
                Elevation = newCell.Elevation,
                Latitude = newCell.Location!.Coordinate.Y,
                Longitude = newCell.Location!.Coordinate.X,
                IsVerified = newCell.IsVerified,
                IsFileUploaded = newCell.IsFileUploaded,
                Status = EnumHelper<CellStatusEnum>.GetName(newCell.Status)
            },
            Message = "Cell created successfully.",
            Succeeded = true
        };
    }
}
