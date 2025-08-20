using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.TechnologyBand;

namespace NTC.QoS.Application.Features.TechnologyBand.Requests;

public class CreateTechnologyBandRequest : IRequest<ApiResponse<TechnologyBandDto>>
{
    public required CreateTechnologyBandDto TechnologyBand { get; set; }
}