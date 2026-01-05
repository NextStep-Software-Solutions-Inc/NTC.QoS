using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class GetAllCellularCoveragesRequest : IRequest<PaginatedResponse<CellularCoverageDto>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public List<Guid>? NetworkIds { get; set; }
    public List<Guid>? CellIds { get; set; }
    public required Coordinate Coordinate { get; set; }
    public double Distance { get; set; }
    public List<Guid>? BarangayIds { get; set; }
    public List<Guid>? MunicipalityIds { get; set; }
    public List<Guid>? ProvinceIds { get; set; }
    public List<Guid>? RegionIds { get; set; }
    
    // V2 Parameters
    public List<Guid>? BandIds { get; set; }
    public List<Guid>? TechnologyIds { get; set; }
    public List<Guid>? ServiceProviderIds { get; set; }
    public List<Guid>? CellSectorIds { get; set; }
}
