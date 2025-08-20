using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Requests;

public class GetOrganizationByIdRequest(Guid id) : IRequest<ApiResponse<OrganizationDto>>
{
    public Guid Id { get; set; } = id;
}