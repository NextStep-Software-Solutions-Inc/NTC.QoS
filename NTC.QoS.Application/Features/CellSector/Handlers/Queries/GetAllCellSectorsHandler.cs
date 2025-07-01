using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Queries;

public class GetAllCellSectorsHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository) : IRequestHandler<GetAllCellSectorsRequest, PaginatedResponse<CellSectorDto>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;

    public async Task<PaginatedResponse<CellSectorDto>> Handle(GetAllCellSectorsRequest request, CancellationToken cancellationToken)
    {
        var cellSectorQuery = _cellSectorRepository.Query();

        cellSectorQuery = cellSectorQuery
            .Where(cs => request.CellIds == null || (request.CellIds.Count > 0 && request.CellIds.Contains(cs.CellId)));

        var cellSectorDtoQuery = cellSectorQuery
            .Select(cs => new CellSectorDto
            {
                Id = cs.Id,
                Cell = new()
                {
                    Id = cs.Cell.Id,
                    Name = cs.Cell.Name,
                    Group = cs.Cell.Group,
                    Description = cs.Cell.Description,
                    Address = cs.Cell.Address,
                    LAC = cs.Cell.LAC,
                    TAC = cs.Cell.TAC,
                    CID = cs.Cell.CID,
                },
                AntennaType = cs.AntennaType.ToString(),
                AntennaHeight = cs.AntennaHeight,
                Azimuth = cs.Azimuth,
                BeamTilt = cs.BeamTilt
            });

        return await _cellSectorRepository.PaginateAsync(cellSectorDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
