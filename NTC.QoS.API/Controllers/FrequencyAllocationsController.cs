using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.FrequencyAllocation.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("frequency-allocations", Name = "frequency-allocations")]
[Authorize]
public class FrequencyAllocationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Get a paginated list of regulatory frequency allocations.
    /// Supports filtering by provider, active status, and a name search.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.ViewRegulatoryAllocations)]
    public async Task<ActionResult<PaginatedResponse<FrequencyAllocationDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetAllFrequencyAllocationsRequest
        {
            PageNumber  = pageNumber,
            PageSize    = pageSize,
            ProviderIds = providerIds,
            IsActive    = isActive,
            Search      = search,
        });
        return Ok(result);
    }

    /// <summary>
    /// Get a single regulatory frequency allocation by its ID.
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewRegulatoryAllocations)]
    public async Task<ActionResult<ApiResponse<FrequencyAllocationDto>>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetFrequencyAllocationByIdRequest(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Create a new regulatory frequency allocation.
    /// Each Provider + UpLink + DownLink combination must be unique.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.CreateRegulatoryAllocations)]
    public async Task<ActionResult<ApiResponse<FrequencyAllocationDto>>> Create(
        [FromBody] CreateFrequencyAllocationDto createDto)
    {
        var result = await _mediator.Send(new CreateFrequencyAllocationRequest { FrequencyAllocation = createDto });
        if (!result.Succeeded)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Patch-update a regulatory frequency allocation.
    /// Only provided fields are updated; omit fields to leave them unchanged.
    /// </summary>
    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateRegulatoryAllocations)]
    public async Task<ActionResult<ApiResponse<FrequencyAllocationDto>>> Update(
        Guid id,
        [FromBody] UpdateFrequencyAllocationDto updateDto)
    {
        var result = await _mediator.Send(new UpdateFrequencyAllocationByIdRequest { Id = id, FrequencyAllocation = updateDto });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Soft-delete a regulatory frequency allocation.
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteRegulatoryAllocations)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteFrequencyAllocationByIdRequest(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }
}
