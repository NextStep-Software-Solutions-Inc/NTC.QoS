using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Application.Features.Region.Handlers.Queries;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("provinces", Name = "provinces")]
public class ProvincesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvincesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
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
    [HttpGet("id")]
    public async Task<ActionResult<ApiResponse<ProvinceDto>>> GetProvinceById(Guid provinceId)
    {
        var request = new GetProvinceByIdRequest(provinceId);
        var province = await _mediator.Send(request);
        return province.Succeeded ? Ok(province) : BadRequest(province);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateProvinceDto>>> Post(CreateProvinceDto provinceDto)
    {
        var province = await _mediator.Send(new CreateProvinceRequest
        {
            Province = provinceDto
        });

        return province.Succeeded ? Ok(province) : BadRequest(province);
    }
    [HttpDelete("id")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid provinceId)
    {
        var request = new DeleteProvinceByIdRequest(provinceId);
        var province = await _mediator.Send(request);

        return province.Succeeded ? Ok(province) : BadRequest(province);
    }

    [HttpPut("id")]
    public async Task<ActionResult<ApiResponse<UpdateProvinceDto>>> Put(Guid provinceId, UpdateProvinceDto updateProvinceDto)
    {
        var province = await _mediator.Send(new UpdateProvinceRequest
        {
            Id = provinceId,
            Province = updateProvinceDto
        });

        return province.Succeeded ? Ok(province) : BadRequest(province);
    }
}
