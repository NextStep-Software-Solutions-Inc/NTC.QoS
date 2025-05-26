using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Queries;

public class GetAllCellSectorsHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository) : IRequestHandler<GetAllCellSectorsRequest, PaginatedResponse<CellSectorDto>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;

    public async Task<PaginatedResponse<CellSectorDto>> Handle(GetAllCellSectorsRequest request, CancellationToken cancellationToken)
    {
        var cellSectorQuery = _cellSectorRepository.Query(cs => !cs.IsDeleted);

        cellSectorQuery = cellSectorQuery.Where(cs => request.CellIds == null || (request.CellIds.Count > 0 && request.CellIds.Contains(cs.CellId)));

        var cellSectorDtoQuery = cellSectorQuery
            .Select(cs => new CellSectorDto
            {
                Id = cs.Id,
                CellId = cs.CellId,
                AntennaType = EnumHelper<AntennaTypeEnum>.GetName(cs.AntennaType),
                AntennaHeight = cs.AntennaHeight,
                Azimuth = cs.Azimuth,
                BeamTilt = cs.BeamTilt
            });

        return await _cellSectorRepository.PaginateAsync(cellSectorDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
