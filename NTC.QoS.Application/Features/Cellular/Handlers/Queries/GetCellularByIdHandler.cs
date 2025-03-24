using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Queries;

public class GetCellularByIdHandler(ICellularRepository repository) : IRequestHandler<GetCellularByIdRequest, ApiResponse<GetCellularDto>>
{
    private readonly ICellularRepository _repository = repository;

    public async Task<ApiResponse<GetCellularDto>> Handle(GetCellularByIdRequest request, CancellationToken cancellationToken)
    {
        var cellular = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (cellular is null)
        {
            return new ApiResponse<GetCellularDto>
            {
                Succeeded = false,
                Message = "Cellular record not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Cellular record with the specified ID does not exist."] }
                }
            };
        }

        return new ApiResponse<GetCellularDto>
        {
            Succeeded = true,
            Message = "Cellular record retrieved successfully.",
            Data = new GetCellularDto
            {
                Id = cellular.Id,
                Barangay = new CommonCellularDto
                {
                    Id = cellular.BarangayId,
                    Name = cellular.Barangay?.Name ?? string.Empty,
                    Description = cellular.Barangay?.Description ?? string.Empty
                },
                Cell = new CellCellularDto
                {
                    Id = cellular.CellId,
                    Name = cellular.Cell?.Name ?? string.Empty,
                },
                ServiceProvider = new ServiceProviderCellularDto
                {
                    Id = cellular.ServiceProviderId,
                    Provider = new CommonCellularDto
                    {
                        Id = cellular.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = cellular.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = cellular.ServiceProvider?.Provider?.Description ?? string.Empty
                    },
                    Service = new ServiceCellularDto
                    {
                        Id = cellular.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = cellular.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = cellular.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    }
                },
                GenerationBand = new GenerationBandCellularDto
                {
                    Id = cellular.GenerationBandId,
                    Generation = new CommonCellularDto
                    {
                        Id = cellular.GenerationBand?.Generation?.Id ?? Guid.Empty,
                        Name = cellular.GenerationBand?.Generation?.Name ?? string.Empty,
                        Description = cellular.GenerationBand?.Generation?.Description ?? string.Empty
                    },
                    Band = new CommonCellularDto
                    {
                        Id = cellular.GenerationBand?.Band?.Id ?? Guid.Empty,
                        Name = cellular.GenerationBand?.Band?.Name ?? string.Empty,
                        Description = cellular.GenerationBand?.Band?.Description ?? string.Empty
                    }
                },
                File = new FileCellularDto
                {
                    Id = cellular.FileId,
                    Name = cellular.File?.Name ?? string.Empty
                },
                RowNumber = cellular.RowNumber
            }
        };
    }
}