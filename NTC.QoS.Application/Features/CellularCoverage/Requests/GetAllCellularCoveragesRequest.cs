using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Requests;

public class GetAllCellularCoveragesRequest : IRequest<PaginatedResponse<CellularCoverageDto>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public List<Guid>? NetworkIds { get; set; }
    public required Coordinate Coordinate { get; set; }
    public double Distance { get; set; }
}
