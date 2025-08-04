using MediatR;
using NTC.QoS.Domain.Models.DTOs.TechnologyBand;

namespace NTC.QoS.Application.Features.TechnologyBand.Requests;

public class GetAllTechnologyBandsRequest : IRequest<PaginatedResponse<TechnologyBandDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public List<Guid>? Technologies { get; set; }
    public List<Guid>? Bands { get; set; }
}