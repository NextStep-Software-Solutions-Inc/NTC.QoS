using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("organizations", Name = "organizations")]
[Authorize]

public class OrganizationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.CreateOrganizations)]
    public async Task<ActionResult<PaginatedResponse<OrganizationDto>>> CreateOrganization(CreateOrganizationDto organizationDto)
    {
        var organization = await _mediator.Send(new CreateOrganizationRequest
        {
            Organization = organizationDto
        });
        return organization.Succeeded ? Ok(organization) : BadRequest(organization);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewOrganizations)]
    public async Task<ActionResult<PaginatedResponse<OrganizationDto>>> GetAllOrganizations(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllOrganizationsRequest(pageSize, pageNumber));
        return Ok(result);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewOrganizations)]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> GetOrganizationById(Guid id)
    {
        var request = new GetOrganizationByIdRequest(id);
        var role = await _mediator.Send(request);
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateOrganizations)]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> UpdateOrganizationById(Guid id, UpdateOrganizationDto organizationDto)
    {
        var organization = await _mediator.Send(new UpdateOrganizationByIdRequest
        {
            Id = id,
            Organization = organizationDto
        });
        return organization.Succeeded ? Ok(organization) : BadRequest(organization);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteOrganizations)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteOrganizationById(Guid id)
    {
        var organization = await _mediator.Send(new DeleteOrganizationByIdRequest(id));
        return organization .Succeeded ? Ok(organization) : BadRequest(organization);
    }
}
