using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Requests;

public class CreateTechnologyRequest : IRequest<ApiResponse<TechnologyDto>>
{
    public required CreateTechnologyDto Technology { get; set; }
}