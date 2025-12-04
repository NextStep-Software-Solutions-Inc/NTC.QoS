using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Scheduler.Requests.Commands;
using NTC.QoS.Application.Features.Scheduler.Requests.Queries;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Controller for managing connectivity assessment schedulers
/// </summary>
[ApiController]
[Route("api/schedulers")]
[Authorize]
public class SchedulersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SchedulersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all connectivity assessment schedules with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="searchTerm">Optional search term to filter by name or description</param>
    /// <returns>Paginated list of schedulers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SchedulerResponseDto>), 200)]
    public async Task<ActionResult<PaginatedResponse<SchedulerResponseDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var request = new GetAllSchedulersRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm
        };

        var result = await _mediator.Send(request);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific connectivity assessment schedule by ID
    /// </summary>
    /// <param name="id">Scheduler ID</param>
    /// <returns>Scheduler details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SchedulerResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SchedulerResponseDto>> GetById(Guid id)
    {
        var request = new GetSchedulerByIdRequest { Id = id };
        var result = await _mediator.Send(request);

        if (result == null)
        {
            return NotFound(new { message = $"Scheduler with ID {id} not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new connectivity assessment schedule
    /// </summary>
    /// <param name="dto">Scheduler configuration</param>
    /// <returns>Created scheduler</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SchedulerResponseDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<SchedulerResponseDto>> Create([FromBody] CreateSchedulerDto dto)
    {
        var request = new CreateSchedulerRequest { Data = dto };
        var result = await _mediator.Send(request);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing connectivity assessment schedule
    /// </summary>
    /// <param name="id">Scheduler ID</param>
    /// <param name="dto">Updated scheduler configuration</param>
    /// <returns>Updated scheduler</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SchedulerResponseDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<SchedulerResponseDto>> Update(Guid id, [FromBody] UpdateSchedulerDto dto)
    {
        try
        {
            var request = new UpdateSchedulerRequest { Id = id, Data = dto };
            var result = await _mediator.Send(request);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a connectivity assessment schedule
    /// </summary>
    /// <param name="id">Scheduler ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var request = new DeleteSchedulerRequest { Id = id };
        var success = await _mediator.Send(request);

        if (!success)
        {
            return NotFound(new { message = $"Scheduler with ID {id} not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Pause a connectivity assessment schedule
    /// </summary>
    /// <param name="id">Scheduler ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Pause(Guid id)
    {
        var request = new PauseSchedulerRequest { Id = id };
        var success = await _mediator.Send(request);

        if (!success)
        {
            return NotFound(new { message = $"Scheduler with ID {id} not found" });
        }

        return Ok(new { message = "Scheduler paused successfully" });
    }

    /// <summary>
    /// Resume a paused connectivity assessment schedule
    /// </summary>
    /// <param name="id">Scheduler ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Resume(Guid id)
    {
        var request = new ResumeSchedulerRequest { Id = id };
        var success = await _mediator.Send(request);

        if (!success)
        {
            return NotFound(new { message = $"Scheduler with ID {id} not found" });
        }

        return Ok(new { message = "Scheduler resumed successfully" });
    }
}
