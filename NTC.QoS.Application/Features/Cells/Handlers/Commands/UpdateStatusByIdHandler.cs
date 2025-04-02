using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class UpdateStatusByIdHandler(ICellRepository repository) : IRequestHandler<UpdateCellStatusByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _repository = repository;

    public async Task<ApiResponse<CellDto>> Handle(UpdateCellStatusByIdRequest request, CancellationToken cancellationToken)
    {
        var cell = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CellDto>
            {
                Succeeded = false,
                Message = "Cell not found",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCell", ["Cell not found."]
                    }
                }
            };
        }

        cell.Status = EnumHelper<CellStatusEnum>.ParseUserInput(request.Status);

        _repository.Update(cell);
        await _repository.SaveChangesAsync(cancellationToken);

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
            Succeeded = true,
            Message = "Cell status updated successfully."
        };
    }
}
