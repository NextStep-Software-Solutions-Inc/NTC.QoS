using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Requests;

public class GetAllAntennaPatternsRequest : IRequest<PaginatedResponse<AntennaPatternDto>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public string? Search { get; set; }
}