using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Requests;

public class GetNetworkTemplateByIdRequest(Guid id) : IRequest<ApiResponse<NetworkTemplateDto>>
{
    public Guid Id { get; set; } = id;
}
