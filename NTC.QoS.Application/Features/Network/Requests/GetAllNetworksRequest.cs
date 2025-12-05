using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class GetAllNetworksRequest(
    int pageNumber, 
    int pageSize, 
    string? search, 
    Guid? networkTemplateId,
    Guid? technologyId,
    Guid? bandId,
    Guid? locationId,
    string? locationType,
    Guid? systemTypeId,
    Guid? serviceProviderId,
    Guid? cellId) : IRequest<PaginatedResponse<NetworkDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public string? Search { get; set; } = search;
    public Guid? NetworkTemplateId { get; set; } = networkTemplateId;
    public Guid? TechnologyId { get; set; } = technologyId;
    public Guid? BandId { get; set; } = bandId;
    public Guid? LocationId { get; set; } = locationId;
    public string? LocationType { get; set; } = locationType;
    public Guid? SystemTypeId { get; set; } = systemTypeId;
    public Guid? ServiceProviderId { get; set; } = serviceProviderId;
    public Guid? CellId { get; set; } = cellId;
}
