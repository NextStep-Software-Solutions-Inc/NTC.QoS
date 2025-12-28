using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class CreateBarangayCoverageRequest : IRequest<ApiResponse<BarangayCoverageDto>>
{
    public required CreateBarangayCoverageDto Coverage { get; set; }
}
