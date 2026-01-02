using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class UpdateBarangayCoverageRequest : IRequest<ApiResponse<BarangayCoverageDto>>
{
    public Guid BarangayId { get; set; }
    public required UpdateBarangayCoverageDto Coverage { get; set; }
}
