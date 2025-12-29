using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class GetBarangayCoverageByRegionRequest : IRequest<ApiResponse<List<BarangayCoverageDto>>>
{
    public Guid RegionId { get; set; }
}
