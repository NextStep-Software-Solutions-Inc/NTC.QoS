using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class GetAllSpeedtestsRequest : IRequest<PaginatedResponse<SpeedtestDto>>
{
    public GetAllSpeedtestsRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllSpeedtestsRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? BarangayId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? CellId { get; set; }
    public Guid? FileId { get; set; }
    public SignalQualityEnum? SignalQuality { get; set; }
}


