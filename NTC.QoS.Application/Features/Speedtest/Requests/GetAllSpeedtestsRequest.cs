using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class GetAllSpeedtestsRequest(int pageNumber,
                                     int pageSize,
                                     string? search,
                                     string? status,
                                     List<Guid>? fileIds,
                                     List<Guid>? serviceProviderIds,
                                     List<Guid>? barangayIds,
                                     List<Guid>? cellIds,
                                     List<Guid>? generationIds,
                                     List<Guid>? providerIds,
                                     List<Guid>? serviceIds) : IRequest<PaginatedResponse<SpeedtestDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public string? Search { get; set; } = search;
    public string? Status { get; set; } = status;
    public List<Guid>? BarangayIds { get; set; } = barangayIds;
    public List<Guid>? ServiceProviderIds { get; set; } = serviceProviderIds;
    public List<Guid>? GenerationIds { get; set; } = generationIds;
    public List<Guid>? CellIds { get; set; } = cellIds;
    public List<Guid>? FileIds { get; set; } = fileIds;
    public SignalQualityEnum? SignalQuality { get; set; }
    public List<Guid>? ProviderIds { get; set; } = providerIds;
    public List<Guid>? ServiceIds { get; set; } = serviceIds;
}


