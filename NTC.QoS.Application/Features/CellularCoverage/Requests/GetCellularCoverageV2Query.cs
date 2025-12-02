using MediatR;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

/// <summary>
/// Query to get CellularCoverage V2 records with pagination
/// </summary>
public class GetCellularCoverageV2Query : IRequest<PaginatedResponse<CellularCoverageV2Dto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? MunicipalityId { get; set; }
    public Guid? NetworkId { get; set; }
    public Guid? BandId { get; set; }
    public Guid? ServiceProviderId { get; set; }
}
