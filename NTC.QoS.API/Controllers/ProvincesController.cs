using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("provinces", Name = "provinces")]
[Authorize]

public class ProvincesController : ControllerBase
{
    private readonly IMediator _mediator;
    private const int BrowserCacheDuration = 86400;

    public ProvincesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProvinceDto>>> CreateProvince([FromForm] CreateProvinceDto provinceDto)
    {
        var province = await _mediator.Send(new CreateProvinceRequest
        {
            Province = provinceDto
        });

        return province.Succeeded ? Ok(province) : BadRequest(province);
    }

    [HttpGet]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "regionId" })]

    public async Task<ActionResult<PaginatedResponse<ProvinceDto>>> GetAllProvinces(int pageSize = 10, int pageNumber = 1, Guid? regionId = null)
    {
        var request = new GetAllProvincesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            RegionId = regionId,
        };
        var provinces = await _mediator.Send(request);
        return Ok(provinces);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProvinceDto>>> GetProvinceById(Guid id)
    {
        var request = new GetProvinceByIdRequest(id);
        var province = await _mediator.Send(request);
        return province.Succeeded ? Ok(province) : BadRequest(province);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<ProvinceDto>>> UpdateProvinceById(Guid id, [FromForm] UpdateProvinceDto updateProvince)
    {
        var province = await _mediator.Send(new UpdateProvinceRequest { Id = id, Province = updateProvince });
        return province.Succeeded ? Ok(province) : BadRequest(province);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteProvinceByIdRequest(id);
        var province = await _mediator.Send(request);

        return province.Succeeded ? Ok(province) : BadRequest(province);
    }
}
