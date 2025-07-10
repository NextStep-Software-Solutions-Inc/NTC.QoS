using MediatR;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Requests;

public class GetAllCellularsRequest : IRequest<PaginatedResponse<CellularDto>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? CellId { get; set; }
    public Guid? CellSectorId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? GenerationBandId { get; set; }
}
