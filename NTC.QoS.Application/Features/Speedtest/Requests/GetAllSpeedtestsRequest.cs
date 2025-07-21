using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class GetAllSpeedtestsRequest(int pageNumber, int pageSize) : IRequest<PaginatedResponse<SpeedtestDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public string? Search { get; set; }
    public string? Address { get; set; }
    public List<Guid>? BarangayIds { get; set; }
    public List<Guid>? MunicipalityIds { get; set; }
    public List<Guid>? ProvinceIds { get; set; }
    public List<Guid>? RegionIds { get; set; }
    public List<Guid>? ServiceProviderIds { get; set; }
    public List<Guid>? CellIds { get; set; }
    public List<Guid>? GenerationIds { get; set; }
    public List<Guid>? ProviderIds { get; set; }
    public List<Guid>? ServiceIds { get; set; }
    public string? SignalQuality { get; set; }
    public required Coordinate Coordinate { get; set; }
    public double Distance { get; set; }
}

