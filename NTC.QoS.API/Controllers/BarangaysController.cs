using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("barangays", Name = "barangays")]
[Authorize]

public class BarangaysController : ControllerBase
{
    private readonly IMediator _mediator;
    private const int BrowserCacheDuration = 86400;

    public BarangaysController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequirePermission(Permissions.CreateBarangays)]
    public async Task<ActionResult<ApiResponse<BarangayDto>>> CreateBarangay([FromForm] CreateBarangayDto barangayDto)
    {
        var barangay = await _mediator.Send(new CreateBarangayRequest
        {
            Barangay = barangayDto
        });

        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewBarangays)]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "municipalityIds", "name", "municipality", "province" })]
    public async Task<ActionResult<PaginatedResponse<BarangayDto>>> GetAllBarangays(int pageSize = 10,
                                                                                    int pageNumber = 1,
                                                                                    [FromQuery] Guid[]? municipalityIds = default,
                                                                                    string? name = default,
                                                                                    string? municipality = default,
                                                                                    string? province = default)
    {
        var request = new GetAllBarangaysRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            MunicipalityIds = municipalityIds,
            Name = name,
            Municipality = municipality,
            Province = province
        };
        var barangays = await _mediator.Send(request);
        return Ok(barangays);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewBarangays)]
    public async Task<ActionResult<ApiResponse<BarangayDto>>> GetBarangayById(Guid id)
    {
        var request = new GetBarangayByIdRequest(id);
        var barangay = await _mediator.Send(request);
        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateBarangays)]
    public async Task<ActionResult<ApiResponse<BarangayDto>>> UpdateBarangayById(Guid id, [FromForm] UpdateBarangayDto updateBarangay)
    {
        var barangay = await _mediator.Send(new UpdateBarangayByIdRequest(id, updateBarangay));
        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteBarangays)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteBarangayByIdRequest(id);
        var barangay = await _mediator.Send(request);

        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }
}
