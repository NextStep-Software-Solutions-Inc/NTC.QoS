using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class GetAllBarangayCoveragesRequest : IRequest<ApiResponse<List<BarangayCoverageLightDto>>>
{
    public List<Guid>? RegionIds { get; set; }
    public List<Guid>? ProvinceIds { get; set; }
    public List<Guid>? MunicipalityIds { get; set; }
    public List<Guid>? BarangayIds { get; set; }
}
