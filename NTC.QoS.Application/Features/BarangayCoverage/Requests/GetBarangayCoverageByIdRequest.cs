using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class GetBarangayCoverageByIdRequest : IRequest<ApiResponse<BarangayCoverageDto>>
{
    public Guid Id { get; set; }
}
