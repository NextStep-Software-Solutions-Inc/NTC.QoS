using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Organization.Requests;
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
    public async Task<IActionResult> CreateOrganization(CreateOrganizationDto organizationDto)
    {
        var organization = await _mediator.Send(new CreateOrganizationRequest
        {
            Organization = organizationDto
        });
        return organization.Succeeded ? Ok(organization) : BadRequest(organization);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GetOrganizationDto>>> GetAllOrganizations(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllOrganizationsRequest(pageSize, pageNumber));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GetOrganizationDto>>> GetOrganizationById(Guid id)
    {
        var request = new GetOrganizationByIdRequest(id);
        var role = await _mediator.Send(request);
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateOrganizationDto>>> UpdateOrganizationById(Guid id, UpdateOrganizationDto organizationDto)
    {
        var organization = await _mediator.Send(new UpdateOrganizationByIdRequest
        {
            Id = id,
            Organization = organizationDto
        });
        return organization.Succeeded ? Ok(organization) : BadRequest(organization);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteOrganizationById(Guid id)
    {
        var organization = await _mediator.Send(new DeleteOrganizationByIdRequest(id));
        return organization .Succeeded ? Ok(organization) : BadRequest(organization);
    }
}
