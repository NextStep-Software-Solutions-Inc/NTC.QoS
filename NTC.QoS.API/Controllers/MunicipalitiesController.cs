using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("municipalities", Name = "municipalities")]
[Authorize]

public class MunicipalitiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private const int BrowserCacheDuration = 86400;

    public MunicipalitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequirePermission(Permissions.CreateMunicipalities)]
    public async Task<ActionResult<ApiResponse<MunicipalityDto>>> CreateMunicipality([FromForm] CreateMunicipalityDto createMunicipality)
    {
        var municipality = await _mediator.Send(new CreateMunicipalityRequest
        {
            Municipality = createMunicipality
        });

        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewMunicipalities)]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "provinceId", "provinceIds", "regionIds", "isWiredCovered", "isWirelessCovered" })]
    public async Task<ActionResult<PaginatedResponse<MunicipalityDto>>> GetAllMunicipalities(
        int pageSize = 10, 
        int pageNumber = 1, 
        [FromQuery] Guid[]? provinceIds = null,
        Guid? provinceId = null,
        [FromQuery] Guid[]? regionIds = null,
        bool? isWiredCovered = null,
        bool? isWirelessCovered = null)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var request = new GetAllMunicipalitiesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ProvinceIds = provinceIds,
            ProvinceId = provinceId,
            RegionIds = regionIds,
            IsWiredCovered = isWiredCovered,
            IsWirelessCovered = isWirelessCovered
        };
#pragma warning restore CS0618 // Type or member is obsolete
        var municipalities = await _mediator.Send(request);
        return municipalities.Succeeded ? Ok(municipalities) : BadRequest(municipalities);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewMunicipalities)]
    public async Task<ActionResult<ApiResponse<MunicipalityDto>>> GetMunicipalityById(Guid id)
    {
        var request = new GetMunicipalityByIdRequest(id);
        var municipality = await _mediator.Send(request);
        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateMunicipalities)]
    public async Task<ActionResult<ApiResponse<MunicipalityDto>>> UpdateMunicipalityById(Guid id, [FromForm] UpdateMunicipalityDto updateMunicipality)
    {
        var municipality = await _mediator.Send(new UpdateMunicipalityByIdRequest(id, updateMunicipality));
        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteMunicipalities)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteMunicipalityByIdRequest(id);
        var municipality = await _mediator.Send(request);

        return municipality.Succeeded ? Ok(municipality) : BadRequest(municipality);
    }
}
