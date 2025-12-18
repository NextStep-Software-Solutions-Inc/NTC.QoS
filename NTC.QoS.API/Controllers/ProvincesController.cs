using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Authorization;
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
    [RequirePermission(Permissions.CreateProvinces)]
    public async Task<ActionResult<ApiResponse<ProvinceDto>>> CreateProvince([FromForm] CreateProvinceDto provinceDto)
    {
        var province = await _mediator.Send(new CreateProvinceRequest
        {
            Province = provinceDto
        });

        return province.Succeeded ? Ok(province) : BadRequest(province);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewProvinces)]
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
    [RequirePermission(Permissions.ViewProvinces)]
    public async Task<ActionResult<ApiResponse<ProvinceDto>>> GetProvinceById(Guid id)
    {
        var request = new GetProvinceByIdRequest(id);
        var province = await _mediator.Send(request);
        return province.Succeeded ? Ok(province) : BadRequest(province);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateProvinces)]
    public async Task<ActionResult<ApiResponse<ProvinceDto>>> UpdateProvinceById(Guid id, [FromForm] UpdateProvinceDto updateProvince)
    {
        var province = await _mediator.Send(new UpdateProvinceRequest { Id = id, Province = updateProvince });
        return province.Succeeded ? Ok(province) : BadRequest(province);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteProvinces)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteProvinceByIdRequest(id);
        var province = await _mediator.Send(request);

        return province.Succeeded ? Ok(province) : BadRequest(province);
    }
}
