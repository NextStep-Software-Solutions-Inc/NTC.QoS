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

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MunicipalityDto>>> GetAllMunicipalities(int pageSize = 10, int pageNumber = 1)
    {
        var request = new GetAllMunicipalitiesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var municipalities = await _mediator.Send(request);
        return Ok(municipalities);
    }
    [HttpGet("id")]
    public async Task<ActionResult<ApiResponse<MunicipalityDto>>> GetMunicipalityById(Guid municipalityId)
    {
        var request = new GetMunicipalityByIdRequest(municipalityId);
        var municipality = await _mediator.Send(request);
        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateMunicipalityDto>>> Post(CreateMunicipalityDto municipalityDto)
    {
        var municipality = await _mediator.Send(new CreateMunicipalityRequest
        {
            Municipality = municipalityDto
        });

        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }
    [HttpDelete("id")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid municipalityId)
    {
        var request = new DeleteMunicipalityByIdRequest(municipalityId);
        var municipality = await _mediator.Send(request);

        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }

    [HttpPut("id")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Municipality>>> UpdateMunicipalityById(Guid id, UpdateMunicipalityDto updateMunicipality)
    {
        var municipality = await _mediator.Send(new UpdateMunicipalityByIdRequest(id, new UpdateMunicipalityDto
        {
            Name = updateMunicipality.Name,
            Description = updateMunicipality.Description,
            IsCity = updateMunicipality.IsCity,
        }));
        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }
}
