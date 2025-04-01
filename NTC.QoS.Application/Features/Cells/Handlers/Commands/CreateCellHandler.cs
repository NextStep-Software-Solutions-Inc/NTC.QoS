using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Zone;

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
                Id = newCell.Id,
                Name = newCell.Name,
                Group = newCell.Group,
                File = new FileDto
                {
                    Id = newCell.FileId ?? Guid.Empty,
                    Name = newCell.File?.Name ?? string.Empty,
                    Path = newCell.File?.Path ?? string.Empty
                },
                Description = newCell.Description ?? string.Empty,
                Zone = new ZoneDto
                {
                    Id = newCell.ZoneId,
                    Name = newCell.Zone?.Name ?? string.Empty,
                    Barangay = new BarangayDto
                    {
                        Id = newCell.Zone?.BarangayId ?? Guid.Empty,
                        Name = newCell.Zone?.Barangay?.Name ?? string.Empty,
                        Description = newCell.Zone?.Barangay?.Description,
                        Municipality = new MunicipalityDto
                        {
                            Id = newCell.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                            IsCity = newCell.Zone?.Barangay?.Municipality?.IsCity ?? false,
                            Name = newCell.Zone?.Barangay?.Municipality?.Name ?? string.Empty,
                            Description = newCell.Zone?.Barangay?.Municipality?.Description,
                            Province = new ProvinceDto
                            {
                                Id = newCell.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                                Name = newCell.Zone?.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                                Description = newCell.Zone?.Barangay?.Municipality?.Province?.Description,
                                Region = new RegionDto
                                {
                                    Id = newCell.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                    Name = newCell.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                    Description = cell?.Zone?.Barangay?.Municipality?.Province?.Region?.Description
                                }
                            }
                        }
                    }
                },
                ServiceProvider = new ServiceProviderDto
                {
                    Id = newCell.ServiceProviderId,
                    Service = new ServiceDto()
                    {
                        Id = newCell.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = newCell.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = newCell.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new ProviderDto()
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
                CoorLatitude = newCell.CoorLatitude,
                CoorLongitude = newCell.CoorLongitude,
                IsVerified = newCell.IsVerified,
                Status = EnumHelper<CellStatusEnum>.GetName(newCell.Status)
            },
            Message = "Cell created successfully.",
            Succeeded = true
        };
    }
}
