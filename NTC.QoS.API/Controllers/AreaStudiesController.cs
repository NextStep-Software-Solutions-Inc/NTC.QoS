using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("area-studies", Name = "area-studies")]
[Authorize]

public class AreaStudiesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.CreateAreaStudyTypes)]
    public async Task<ActionResult<ApiResponse<AreaStudyDto>>> CreateAreaStudy(CreateAreaStudyDto areaStudyDto)
    {
        var areaStudy = await _mediator.Send(new CreateAreaStudyRequest(areaStudyDto));
        return areaStudy.Succeeded ? Ok(areaStudy) : BadRequest(areaStudy);
    }
    
    [HttpGet]
    [RequirePermission(Permissions.ViewAreaStudyTypes)]
    public async Task<ActionResult<PaginatedResponse<AreaStudyDto>>> GetAllAreaStudies([FromQuery] int pageNumber = 1,
                                                                                       [FromQuery] int pageSize = 10,
                                                                                       [FromQuery] string? search = null)
    {
        var areaStudies = await _mediator.Send(new GetAllAreaStudiesRequest(pageNumber, pageSize, search));
        return areaStudies.Succeeded ? Ok(areaStudies) : BadRequest(areaStudies);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewAreaStudyTypes)]
    public async Task<ActionResult<ApiResponse<AreaStudyDto>>> GetAreaStudyById(Guid id)
    {
        var areaStudies = await _mediator.Send(new GetAreaStudyByIdRequest(id));
        return areaStudies.Succeeded ? Ok(areaStudies) : BadRequest(areaStudies);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateAreaStudyTypes)]
    public async Task<ActionResult<ApiResponse<AreaStudyDto>>> UpdateAreaStudyById(Guid id, UpdateAreaStudyDto updateAreaStudy)
    {
        var areaStudy = await _mediator.Send(new UpdateAreaStudyByIdRequest(id, updateAreaStudy));
        return areaStudy.Succeeded ? Ok(areaStudy) : BadRequest(areaStudy);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteAreaStudyTypes)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAreaStudyById(Guid id)
    {
        var areaStudy = await _mediator.Send(new DeleteAreaStudyByIdRequest(id));
        return areaStudy.Succeeded ? Ok(areaStudy) : BadRequest(areaStudy);
    }
}