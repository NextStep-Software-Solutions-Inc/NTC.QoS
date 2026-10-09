using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MapTiler;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class InitTilesetIngestCommand : IRequest<ApiResponse<InitTilesetIngestResultDto>>
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? DisplayName { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? BandId { get; set; }
    public Guid? TechnologyId { get; set; }
}
