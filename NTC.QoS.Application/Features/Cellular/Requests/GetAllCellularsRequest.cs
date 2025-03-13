using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Requests;

public class GetAllCellularsRequest : IRequest<PaginatedResponse<GetCellularDto>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? BarangayId { get; set; }
    public Guid? CellId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? GenerationBandId { get; set; }
    public Guid? FileId { get; set; }
}
