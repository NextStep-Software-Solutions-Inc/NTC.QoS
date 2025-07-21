using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Requests;

public class UpdateTechnologyRequest : IRequest<ApiResponse<TechnologyDto>>
{
    public Guid Id { get; set; }
    public UpdateTechnologyDto Technology { get; set; } = null!;
}
