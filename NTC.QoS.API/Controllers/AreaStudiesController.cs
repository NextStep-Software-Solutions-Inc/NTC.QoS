using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.API.Controllers;
[ApiController]
[Route("area-studies", Name = "area-studies")]
public class AreaStudiesController : ControllerBase
{
    private readonly IMediator _mediator;
    public AreaStudiesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AreaStudyDto>>> GetAllAreaStudies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var request = new GetAllAreaStudiesRequest(pageNumber, pageSize, search);
        var areaStudies = await _mediator.Send(request);
        return areaStudies.Succeeded ? Ok(areaStudies) : BadRequest(areaStudies);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AreaStudyDto>>> GetAreaStudyById(Guid id)
    {
        Guid areaStudyId = id;
        var request = new GetAreaStudyByIdRequest(areaStudyId);
        var areaStudies = await _mediator.Send(request);
        return areaStudies.Succeeded ? Ok(areaStudies) : BadRequest(areaStudies);
    }
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.AreaStudy>>> UpdateAreaStudyById(Guid id, AreaStudyDto updateAreaStudy)
    {
        var areaStudy = await _mediator.Send(new UpdateAreaStudyByIdRequest(id, new AreaStudyDto
        {
            Name = updateAreaStudy.Name,
            Description = updateAreaStudy.Description
        }));
        return areaStudy.Succeeded ? Ok(areaStudy) : BadRequest(areaStudy);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Domain.Entities.AreaStudy>>> Post(AreaStudyDto areaStudyDto)
    {
        var areaStudy = await _mediator.Send(new CreateAreaStudyRequest
        {
            AreaStudy = areaStudyDto
        });
        return areaStudy.Succeeded ? Ok(areaStudy) : BadRequest(areaStudy);
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteAreaStudyByIdRequest(id);
        var areaStudy = await _mediator.Send(request);
        return areaStudy.Succeeded ? Ok(areaStudy) : BadRequest(areaStudy);
    }
}