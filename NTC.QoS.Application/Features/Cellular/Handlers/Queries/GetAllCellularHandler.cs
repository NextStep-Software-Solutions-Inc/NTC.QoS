using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Queries;

public class GetAllCellularHandler(ICellularRepository repository) : IRequestHandler<GetAllCellularsRequest, PaginatedResponse<GetCellularDto>>
{
    private readonly ICellularRepository _repository = repository;
    public async Task<PaginatedResponse<GetCellularDto>> Handle(GetAllCellularsRequest request, CancellationToken cancellationToken)
    {
        var allCellulars = await _repository.ListAsync(c => !c.IsDeleted, cancellationToken);

        if (request.BarangayId.HasValue)
        {
            allCellulars = [.. allCellulars.Where(c => c.BarangayId == request.BarangayId.Value)];
        }

        if (request.CellId.HasValue)
        {
            allCellulars = [.. allCellulars.Where(c => c.CellId == request.CellId.Value)];
        }

        if (request.ServiceProviderId.HasValue)
        {
            allCellulars = [.. allCellulars.Where(c => c.ServiceProviderId == request.ServiceProviderId.Value)];
        }

        if (request.GenerationBandId.HasValue)
        {
            allCellulars = [.. allCellulars.Where(c => c.GenerationBandId == request.GenerationBandId.Value)];
        }

        if (request.FileId.HasValue)
        {
            allCellulars = [.. allCellulars.Where(c => c.FileId == request.FileId)];
        }

        var cellulars = allCellulars.Select(c =>
        {
            return new GetCellularDto
            {
                Id = c.Id,
                Barangay = new CommonCellularDto
                {
                    Id = c.BarangayId,
                    Name = c.Barangay?.Name ?? string.Empty,
                    Description = c.Barangay?.Description ?? string.Empty
                },
                Cell = new CellCellularDto
                {
                    Id = c.CellId,
                    Name = c.Cell?.Name ?? string.Empty
                },
                ServiceProvider = new ServiceProviderCellularDto
                {
                    Id = c.ServiceProviderId,
                    Provider = new CommonCellularDto
                    {
                        Id = c.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = c.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = c.ServiceProvider?.Provider?.Description ?? string.Empty
                    },
                    Service = new ServiceCellularDto
                    {
                        Id = c.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = c.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = c.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    }
                },
                GenerationBand = new GenerationBandCellularDto
                {
                    Id = c.GenerationBandId,
                    Generation = new CommonCellularDto
                    {
                        Id = c.GenerationBand?.GenerationId ?? Guid.Empty,
                        Name = c.GenerationBand?.Generation?.Name ?? string.Empty,
                        Description = c.GenerationBand?.Generation?.Description ?? string.Empty
                    },
                    Band = new CommonCellularDto
                    {
                        Id = c.GenerationBand?.BandId ?? Guid.Empty,
                        Name = c.GenerationBand?.Band?.Name ?? string.Empty,
                        Description = c.GenerationBand?.Band?.Description ?? string.Empty

                    }
                },
                File = new FileCellularDto
                {
                    Id = c.FileId,
                    Name = c.File?.Name ?? string.Empty
                },
                RowNumber = c.RowNumber,
            };
        }).ToList();

        return new PaginatedResponse<GetCellularDto>(cellulars, request.PageNumber, request.PageSize);
    }
}
