using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("studies", Name = "studies")]
public class StudiesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudyDto>>> CreateStudy(CreateStudyDto createStudy)
    {
        var study = await _mediator.Send(new CreateStudyRequest(createStudy));
        return study.Succeeded ? Ok(study) : BadRequest(study);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<StudyDto>>> GetAllStudies(int pageSize = 10, int pageNumber = 1, Guid? areaStudyId = null)
    {
        var request = new GetAllStudiesRequest(pageSize, pageNumber, areaStudyId);
        var studies = await _mediator.Send(request);
        return studies.Succeeded ? Ok(studies) : BadRequest(studies);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudyDto>>> GetStudyById(Guid id)
    {
        var request = new GetStudyByIdRequest(id);
        var study = await _mediator.Send(request);
        return study.Succeeded ? Ok(study) : BadRequest(study);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<StudyDto>>> UpdateStudyById(Guid id, UpdateStudyDto updateStudy)
    {
        var study = await _mediator.Send(new UpdateStudyRequest(id, updateStudy));
        return study.Succeeded ? Ok(study) : BadRequest(study);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteStudyById(Guid id)
    {
        var request = new DeleteStudyByIdRequest(id);
        var study = await _mediator.Send(request);
        return study.Succeeded ? Ok(study) : BadRequest(study);
    }
}
