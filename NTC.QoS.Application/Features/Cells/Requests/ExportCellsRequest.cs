using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class ExportCellsRequest : IRequest<ApiResponse<CellExportResultDto>>
{
    public ReportFormat Format { get; set; } = ReportFormat.Excel;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public List<Guid>? ServiceProviderIds { get; set; }
    public List<Guid>? ProviderIds { get; set; }
    public List<Guid>? ServiceIds { get; set; }
    public List<Guid>? GenerationIds { get; set; }
    public List<Guid>? TechnologyIds { get; set; }
    public List<Guid>? BandIds { get; set; }
    public Coordinate Coordinate { get; set; } = new(0, 0);
    public double Distance { get; set; } = 15000000;
    public List<Guid>? BarangayIds { get; set; }
    public List<Guid>? MunicipalityIds { get; set; }
    public List<Guid>? ProvinceIds { get; set; }
    public List<Guid>? RegionIds { get; set; }
    public bool? IsVerified { get; set; }
}
