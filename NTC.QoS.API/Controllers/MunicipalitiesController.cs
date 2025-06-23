using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("municipalities", Name = "municipalities")]
public class MunicipalitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MunicipalitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MunicipalityDto>>> CreateMunicipality([FromForm] CreateMunicipalityDto createMunicipality)
    {
        var municipality = await _mediator.Send(new CreateMunicipalityRequest
        {
            Municipality = createMunicipality
        });

        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MunicipalityDto>>> GetAllMunicipalities(int pageSize = 10, int pageNumber = 1, Guid? provinceId = null)
    {
        var request = new GetAllMunicipalitiesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ProvinceId = provinceId,
        };
        var municipalities = await _mediator.Send(request);
        return municipalities.Succeeded ? Ok(municipalities) : BadRequest(municipalities);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MunicipalityDto>>> GetMunicipalityById(Guid id)
    {
        var request = new GetMunicipalityByIdRequest(id);
        var municipality = await _mediator.Send(request);
        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<MunicipalityDto>>> UpdateMunicipalityById(Guid id, UpdateMunicipalityDto updateMunicipality)
    {
        var municipality = await _mediator.Send(new UpdateMunicipalityByIdRequest(id, updateMunicipality));
        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteMunicipalityByIdRequest(id);
        var municipality = await _mediator.Send(request);

        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }
}
