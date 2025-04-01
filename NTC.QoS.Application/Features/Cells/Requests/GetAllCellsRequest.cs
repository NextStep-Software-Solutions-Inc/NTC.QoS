using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetAllCellsRequest(int pageNumber,
                                int pageSize,
                                string? search,
                                string? status,
                                Guid? fileId,
                                Guid? zoneId,
                                Guid? serviceProviderId,
                                Guid? providerId,
                                Guid? serviceId) : IRequest<PaginatedResponse<CellDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public string? Search { get; set; } = search;
    public string? Status { get; set; } = status;
    public Guid? FileId { get; set; } = fileId;
    public Guid? ZoneId { get; set; } = zoneId;
    public Guid? ServiceProviderId { get; set; } = serviceProviderId;
    public Guid? ProviderId { get; set; } = providerId;
    public Guid? ServiceId { get; set; } = serviceId;
}
