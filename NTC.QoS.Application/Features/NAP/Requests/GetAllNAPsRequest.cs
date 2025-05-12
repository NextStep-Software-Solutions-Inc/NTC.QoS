using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class GetAllNAPsRequest(int pageSize, int pageNumber) : IRequest<PaginatedResponse<NAPDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? ServiceProviderId { get; set; }
    public required Coordinate Coordinate { get; set; }
    public double Distance { get; set; }
}