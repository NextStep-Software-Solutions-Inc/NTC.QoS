using MediatR;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Requests;

public class GetAllSectorsRequest(int pageNumber,
                                  int pageSize,
                                  Guid? networkId,
                                  Guid? mimoId,
                                  Guid? cellSectorId,
                                  Guid? txAntennaPatternId,
                                  Guid? rxAntennaPatternId) : IRequest<PaginatedResponse<SectorDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public Guid? NetworkId { get; set; } = networkId;
    public Guid? MIMOId { get; set; } = mimoId;
    public Guid? CellSectorId { get; set; } = cellSectorId;
    public Guid? TxAntennaPatternId { get; set; } = txAntennaPatternId;
    public Guid? RxAntennaPatternId { get; set; } = rxAntennaPatternId;
}
