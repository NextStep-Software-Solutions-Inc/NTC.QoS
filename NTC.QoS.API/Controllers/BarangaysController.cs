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

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BarangayDto>>> GetAllBarangays(int pageSize = 10, int pageNumber = 1, Guid? municipalityId = null)
    {
        var request = new GetAllBarangaysRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            MunicipalityId = municipalityId,
        };
        var barangays = await _mediator.Send(request);
        return Ok(barangays);
    }
    [HttpGet("id")]
    public async Task<ActionResult<ApiResponse<BarangayDto>>> GetBarangayById(Guid barangayId)
    {
        var request = new GetBarangayByIdRequest(barangayId);
        var barangay = await _mediator.Send(request);
        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateBarangayDto>>> Post(CreateBarangayDto barangayDto)
    {
        var barangay = await _mediator.Send(new CreateBarangayRequest
        {
            Barangay = barangayDto
        });

        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }
    [HttpDelete("id")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid barangayId)
    {
        var request = new DeleteBarangayByIdRequest(barangayId);
        var barangay = await _mediator.Send(request);

        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }

    [HttpPut("id")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Barangay>>> UpdateBarangayById(Guid barangayId, UpdateBarangayDto updateBarangay)
    {
        var barangay = await _mediator.Send(new UpdateBarangayByIdRequest(barangayId, new UpdateBarangayDto
        {
            Name = updateBarangay.Name,
            Description = updateBarangay.Description,
        }));
        return barangay.Succeeded ? Ok(barangay) : BadRequest(barangay);
    }
}
