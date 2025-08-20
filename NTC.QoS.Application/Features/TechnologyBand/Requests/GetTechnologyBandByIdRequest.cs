using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.TechnologyBand;

namespace NTC.QoS.Application.Features.TechnologyBand.Requests;

public class GetTechnologyBandByIdRequest(Guid id) : IRequest<ApiResponse<TechnologyBandDto>>
{
    public Guid Id { get; set; } = id;
}