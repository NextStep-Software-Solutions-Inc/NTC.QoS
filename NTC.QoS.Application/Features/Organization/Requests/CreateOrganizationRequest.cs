using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Requests;

public class CreateOrganizationRequest : IRequest<ApiResponse<OrganizationDto>>
{
    public required CreateOrganizationDto Organization { get; set; }
}
