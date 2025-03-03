using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Requests;

public class UpdateLinkByIdRequest : IRequest<ApiResponse<UpdateLinkDto>>
{
    public Guid Id { get; set; }
    public required UpdateLinkDto Link { get; set; }
}
