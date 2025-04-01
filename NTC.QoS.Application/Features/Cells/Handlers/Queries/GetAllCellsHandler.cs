
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
using System.Reflection;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetAllCellsHandler(ICellRepository repository) : IRequestHandler<GetAllCellsRequest, PaginatedResponse<CellDto>>
{
    private readonly ICellRepository _repository = repository;

    public async Task<PaginatedResponse<CellDto>> Handle(GetAllCellsRequest request, CancellationToken cancellationToken)
    {
        var allCells = await _repository.ListAsync(c => !c.IsDeleted, cancellationToken);

        if (!string.IsNullOrEmpty(request.Search))
            {
                string[] propertiesToSearch = {
                    "Name",
                    "Description",
                    "Group",
                    "Zone.Name",
                    "Zone.Description",
                    "Zone.Barangay.Name",
                    "Zone.Barangay.Municipality.Name",
                    "Zone.Barangay.Municipality.Province.Name",
                    "Zone.Barangay.Municipality.Province.Region.Name"
                    // Add other properties (including FK properties) here
                };

                allCells = [.. allCells.Where(cell =>
                    propertiesToSearch.Any(propertyName =>
                    {
                        object? propertyValue = cell;
                        string[] propertyParts = propertyName.Split('.');

                        foreach (string part in propertyParts)
                        {
                            if (propertyValue is null)
                            {
                                return false;
                            }

                            PropertyInfo? property = propertyValue.GetType().GetProperty(part);
                            if (property is null)
                            {
                                return false;
                            }

                            propertyValue = property.GetValue(propertyValue);
                        }

                        if (propertyValue is string stringValue)
                        {
                            return stringValue is not null 
                                && stringValue.Contains(request.Search, StringComparison.OrdinalIgnoreCase);
                        }

                        return false;
                    })
                )];
            }

        allCells = [.. allCells
            .Where(c => string.IsNullOrEmpty(request.Status) || c.Status == EnumHelper<CellStatusEnum>.ParseUserInput(request.Status))
            .Where(c => !request.FileId.HasValue || c.FileId == request.FileId)
            .Where(c => !request.ZoneId.HasValue || c.ZoneId == request.ZoneId)
            .Where(c => !request.ServiceProviderId.HasValue || c.ServiceProviderId == request.ServiceProviderId)
            .Where(cell => request.ServiceId is null || cell.ServiceProvider!.ServiceId == request.ServiceId)
            .Where(cell => request.ServiceId is null || cell.ServiceProvider!.ServiceId == request.ServiceId)
        ];

        var cellDtos = allCells.Select(cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            File = new FileDto
            {
                Id = cell.FileId ?? Guid.Empty,
                Name = cell.File?.Name ?? string.Empty,
                Path = cell.File?.Path ?? string.Empty
            },
            Description = cell.Description ?? string.Empty,
            Zone = new ZoneDto
            {
                Id = cell.ZoneId,
                Name = cell.Zone?.Name ?? string.Empty,
                Barangay = new BarangayDto
                {
                    Id = cell.Zone?.BarangayId ?? Guid.Empty,
                    Name = cell.Zone?.Barangay?.Name ?? string.Empty,
                    Description = cell.Zone?.Barangay?.Description,
                    Municipality = new MunicipalityDto
                    {
                        Id = cell.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                        IsCity = cell.Zone?.Barangay?.Municipality?.IsCity ?? false,
                        Name = cell.Zone?.Barangay?.Municipality?.Name ?? string.Empty,
                        Description = cell.Zone?.Barangay?.Municipality?.Description,
                        Province = new ProvinceDto
                        {
                            Id = cell.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = cell.Zone?.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = cell.Zone?.Barangay?.Municipality?.Province?.Description,
                            Region = new RegionDto
                            {
                                Id = cell.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                Name = cell.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = cell?.Zone?.Barangay?.Municipality?.Province?.Region?.Description
                            }
                        }
                    }
                }
            },
            ServiceProvider = new ServiceProviderDto
            {
                Id = cell.ServiceProviderId,
                Service = new ServiceDto()
                {
                    Id = cell.ServiceProvider?.Service?.Id ?? Guid.Empty,
                    Name = cell.ServiceProvider?.Service?.Name ?? string.Empty,
                    Type = cell.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                },
                Provider = new ProviderDto()
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
        }).ToList();

        if (cellDtos.Count is 0)
        {
            return new PaginatedResponse<CellDto>(new List<CellDto>(), request.PageNumber, request.PageSize)
            {
                Message = "No records found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCells", ["No cells found."]
                    }
                }
            };
        }
        return new PaginatedResponse<CellDto>(cellDtos, request.PageNumber, request.PageSize)
        {
            Data = cellDtos,
            Message = "Cells retrieved successfully.",
            Succeeded = true
        };
    }
}
