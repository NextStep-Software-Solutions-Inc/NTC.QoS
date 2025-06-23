using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("barangays", Name = "barangays")]
public class BarangaysController : ControllerBase
{
    private readonly IMediator _mediator;

    public BarangaysController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BarangayDto>>> CreateBarangay([FromForm] CreateBarangayDto barangayDto)
    {
        var barangay = await _mediator.Send(new CreateBarangayRequest
        {
            Barangay = barangayDto
        });

        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }

    [HttpGet]
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
            Province= province
        };
        var barangays = await _mediator.Send(request);
        return Ok(barangays);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BarangayDto>>> GetBarangayById(Guid id)
    {
        var request = new GetBarangayByIdRequest(id);
        var barangay = await _mediator.Send(request);
        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<BarangayDto>>> UpdateBarangayById(Guid id, [FromForm] UpdateBarangayDto updateBarangay)
    {
        var barangay = await _mediator.Send(new UpdateBarangayByIdRequest(id, updateBarangay));
        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteBarangayByIdRequest(id);
        var barangay = await _mediator.Send(request);

        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }
}
