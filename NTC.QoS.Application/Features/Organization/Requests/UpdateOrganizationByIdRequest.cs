using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Requests;

public class UpdateOrganizationByIdRequest : IRequest<ApiResponse<UpdateOrganizationDto>>
{
    public Guid Id { get; set; }
    public required UpdateOrganizationDto Organization { get; set; }
}
