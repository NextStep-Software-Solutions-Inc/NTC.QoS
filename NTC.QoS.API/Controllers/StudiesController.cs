using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("studies", Name = "studies")]
public class StudiesController : ControllerBase
{
    private readonly IMediator _mediator;
    public StudiesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<StudyDto>>> GetAllStudies(int pageSize = 10, int pageNumber = 1)
    {
        var request = new GetAllStudiesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        var studies = await _mediator.Send(request);
        return Ok(studies);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudyDto>>> GetStudyById(Guid id)
    {
        var request = new GetStudyByIdRequest(id);
        var study = await _mediator.Send(request);
        return study.Succeeded ? Ok(study) : BadRequest(study);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateStudyDto>>> CreateStudy(CreateStudyDto studyDto)
    {
        var study = await _mediator.Send(new CreateStudyRequest(studyDto));

        return study.Succeeded ? Ok(study) : BadRequest(study);
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteStudyByIdRequest(id);
        var study = await _mediator.Send(request);
        return study.Succeeded ? Ok(study) : BadRequest(study);
    }
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateStudyDto>>> Put(Guid id, UpdateStudyDto updateStudy)
    {
        var request = await _mediator.Send(new UpdateStudyRequest(id, updateStudy));
        return request.Succeeded ? Ok(request) : BadRequest(request);
    }
}
