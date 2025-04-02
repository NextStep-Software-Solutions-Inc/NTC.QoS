using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("study-municipalities", Name = "study-municipalities")]

public class StudyMunicipalitiesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudyMunicipalityDto>>> CreateStudyMunicipality(CreateStudyMunicipalityDto createStudyMunicipality)
    {
        var studyMunicipality = await _mediator.Send(new CreateStudyMunicipalityRequest(createStudyMunicipality));
        return studyMunicipality.Succeeded ? Ok(studyMunicipality) : BadRequest(studyMunicipality);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<StudyMunicipalityDto>>> GetAllStudyMunicipalitys(int pageSize = 10,
                                                                                                  int pageNumber = 1,
                                                                                                  Guid? studyId = null,
                                                                                                  Guid? municipalityId = null)
    {
        var studyMunicipalities = await _mediator.Send(new GetAllStudyMunicipalitiesRequest(pageSize, pageNumber, studyId, municipalityId));
        return studyMunicipalities.Succeeded ? Ok(studyMunicipalities) : BadRequest(studyMunicipalities);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudyMunicipalityDto>>> GetStudyMunicipalityById(Guid id)
    {
        var studyMunicipality = await _mediator.Send(new GetStudyMunicipalityByIdRequest(id));
        return studyMunicipality.Succeeded ? Ok(studyMunicipality) : BadRequest(studyMunicipality);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteStudyMunicipalityById(Guid id)
    {
        var studyMunicipality = await _mediator.Send(new DeleteStudyMunicipalityByIdRequest(id));
        return studyMunicipality.Succeeded ? Ok(studyMunicipality) : BadRequest(studyMunicipality);
    }
}
