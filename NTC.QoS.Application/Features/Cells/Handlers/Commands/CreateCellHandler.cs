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
                    Id = newCell.File!.Id,
                    Name = newCell.File.Name,
                    Path = newCell.File.Path
                },
                Description = newCell.Description,
                Zone = new ZoneDto
                {
                    Id = newCell.ZoneId,
                    Name = newCell.Zone!.Name,
                    Barangay = new BarangayDto
                    {
                        Id = newCell.Zone.BarangayId,
                        Name = newCell.Zone.Barangay!.Name,
                        Description = newCell.Zone.Barangay.Description,
                        Municipality = new MunicipalityDto
                        {
                            Id = newCell.Zone.Barangay.MunicipalityId,
                            IsCity = newCell.Zone.Barangay.Municipality!.IsCity,
                            Name = newCell.Zone.Barangay.Municipality!.Name,
                            Description = newCell.Zone.Barangay.Municipality.Description,
                            Province = new ProvinceDto
                            {
                                Id = newCell.Zone.Barangay.Municipality.ProvinceId,
                                Name = newCell.Zone.Barangay.Municipality.Province!.Name,
                                Description = newCell.Zone.Barangay.Municipality.Province.Description,
                                Region = new RegionDto 
                                { 
                                    Id = newCell.Zone.Barangay.Municipality.Province.RegionId,
                                    Name = newCell.Zone.Barangay.Municipality.Province.Region!.Name,
                                    Description = newCell.Zone.Barangay.Municipality.Province.Region!.Description
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
                        Id = newCell.ServiceProvider!.Service!.Id,
                        Name = newCell.ServiceProvider!.Service!.Name,
                        Type = newCell.ServiceProvider!.Service!.Type.ToString()
                    },
                    Provider = new ProviderDto()
                    {
                        Id = newCell.ServiceProvider!.Provider!.Id,
                        Name = newCell.ServiceProvider!.Provider!.Name,
                        Description = newCell.ServiceProvider!.Provider!.Description,
                        LicenseNumber = newCell.ServiceProvider!.Provider!.LicenseNumber,
                        Color = newCell.ServiceProvider!.Provider!.Color
                    }
                },
                CID = newCell.CID,
                SiteId = newCell.SiteId,
                TowerType = newCell.TowerType,
                Elevation = newCell.Elevation,
                TAC = newCell.TAC,
                LAC = newCell.LAC,
                CoorLatitude = newCell.CoorLatitude,
                CoorLongitude = newCell.CoorLongitude,
                Status = EnumHelper<CellStatusEnum>.GetName(newCell.Status)
            },
            Message = "Cell created successfully.",
            Succeeded = true
        };
    }
}
