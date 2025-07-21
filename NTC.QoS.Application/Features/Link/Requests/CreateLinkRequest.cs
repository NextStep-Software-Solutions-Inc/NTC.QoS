using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Requests;

public class CreateLinkRequest : IRequest<ApiResponse<LinkDto>>
{
    public required CreateLinkDto Link { get; set; }
}