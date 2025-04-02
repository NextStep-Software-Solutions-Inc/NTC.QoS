using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class CreateCellHandler(ICellRepository cellRepository, IFileRepository fileRepository, IZoneRepository zoneRepository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateCellRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IZoneRepository _zoneRepository = zoneRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<CellDto>> Handle(CreateCellRequest request, CancellationToken cancellationToken)
    {
        var cell = await _cellRepository.FindAsync(c => c.Name == request.Cell.Name && !c.IsDeleted, cancellationToken);
        if (cell is not null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Cell with the same name already exists."]
                    }
                }
            };
        }

        Guid? fileId = null;
        if (request.Cell.FileId is not null)
        {
            var file = await _fileRepository.GetByIdAsync(request.Cell.FileId.Value, cancellationToken);
            if (file is null)
            {
                return new ApiResponse<CellDto>
                {
                    Message = "File not found.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "NullFile", ["File not found."]
                        }
                    }
                };
            }
            fileId = file.Id;
        }

        var zone = await _zoneRepository.GetByIdAsync(request.Cell.ZoneId, cancellationToken);
        if (zone is null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Zone not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullZone", ["Zone not found."]
                    }
                }
            };
        }

        var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.Cell.ServiceProviderId, cancellationToken);
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
            FileId = fileId,
            Description = request.Cell.Description,
            ZoneId = zone.Id,
            ServiceProviderId = serviceProvider.Id,
            CID = request.Cell.CID,
            SiteId = request.Cell.SiteId,
            TowerType = request.Cell.TowerType,
            Elevation = request.Cell.Elevation,
            TAC = request.Cell.TAC,
            LAC = request.Cell.LAC,
            CoorLatitude = request.Cell.CoorLatitude,
            CoorLongitude = request.Cell.CoorLongitude,
            Status = EnumHelper<CellStatusEnum>.GetValue(request.Cell.Status)
        };

        _cellRepository.Add(newCell);
        await _cellRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CellDto>
        {
            Data = new CellDto
            {
                Id = cell.Id,
                Name = cell.Name,
                Group = cell.Group,
                File = new()
                {
                    Id = cell.FileId ?? Guid.Empty,
                    Name = cell.File?.Name ?? string.Empty,
                    Path = cell.File?.Path ?? string.Empty
                },
                Description = cell.Description ?? string.Empty,
                Zone = new()
                {
                    Id = cell.ZoneId,
                    Name = cell.Zone?.Name ?? string.Empty,
                    Barangay = new()
                    {
                        Id = cell.Zone?.BarangayId ?? Guid.Empty,
                        Name = cell.Zone?.Barangay?.Name ?? string.Empty,
                        Description = cell.Zone?.Barangay?.Description,
                        Municipality = new()
                        {
                            Id = cell.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                            IsCity = cell.Zone?.Barangay?.Municipality?.IsCity ?? false,
                            Name = cell.Zone?.Barangay?.Municipality?.Name ?? string.Empty,
                            Description = cell.Zone?.Barangay?.Municipality?.Description,
                            Province = new()
                            {
                                Id = cell.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                                Name = cell.Zone?.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                                Description = cell.Zone?.Barangay?.Municipality?.Province?.Description,
                                Region = new()
                                {
                                    Id = cell.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                    Name = cell.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                    Description = cell?.Zone?.Barangay?.Municipality?.Province?.Region?.Description
                                }
                            }
                        }
                    }
                },
                ServiceProvider = new()
                {
                    Id = cell.ServiceProviderId,
                    Service = new()
                    {
                        Id = cell.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = cell.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = cell.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new()
                    {
                        Id = cell.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = cell.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = cell.ServiceProvider?.Provider?.Description,
                        LicenseNumber = cell.ServiceProvider?.Provider?.LicenseNumber,
                        Color = cell.ServiceProvider?.Provider?.Color ?? string.Empty
                    }
                },
                CID = cell.CID,
                LAC = cell.LAC,
                TAC = cell.TAC,
                SiteId = cell.SiteId,
                TowerType = cell.TowerType,
                Elevation = cell.Elevation,
                CoorLatitude = cell.CoorLatitude,
                CoorLongitude = cell.CoorLongitude,
                IsVerified = cell.IsVerified,
                Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
            },
            Message = "Cell created successfully.",
            Succeeded = true
        };
    }
}
