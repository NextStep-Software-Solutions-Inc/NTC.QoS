using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.ReportTemplate.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("report-templates", Name = "report-templates")]
[Authorize]
public class ReportTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all active report templates (Id, Title, Description only).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReportTemplateListDto>>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllReportTemplatesRequest());
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get a report template by ID (full details including Instruction).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReportTemplateDto>>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetReportTemplateByIdRequest(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Create a new report template.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportTemplateDto>>> Create([FromBody] CreateTemplateDto dto)
    {
        var result = await _mediator.Send(new CreateReportTemplateRequest(dto));
        return result.Succeeded 
            ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result) 
            : BadRequest(result);
    }

    /// <summary>
    /// Update a report template.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ReportTemplateDto>>> Update(Guid id, [FromBody] UpdateTemplateDto dto)
    {
        var result = await _mediator.Send(new UpdateReportTemplateRequest(id, dto));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Soft delete a report template.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteReportTemplateByIdRequest(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }
}
