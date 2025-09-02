using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("services", Name = "services")]
[Authorize]

public class ServicesController(IMediator mediator) : ControllerBase
{
    
    private const int BrowserCacheDuration = 21600;
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.CreateServices)]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> CreateService([FromBody] CreateServiceDto createService)
    {
        var service = await _mediator.Send(new CreateServiceRequest(createService));
        return service.Succeeded ? Ok(service) : BadRequest(service);
    }


    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<PaginatedResponse<ServiceDto>>> GetAllServices(int pageSize = 10,
                                                                                  int pageNumber = 1,
                                                                                  [FromQuery] List<Guid>? providerIds = null,
                                                                                  [FromQuery] List<string>? types = null)
    {
        var services = await _mediator.Send(new GetAllServicesRequest(pageSize, pageNumber, providerIds, types));
        return services.Succeeded ? Ok(services) : BadRequest(services);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewServices)]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> GetServiceById(Guid id)
    {
        var service = await _mediator.Send(new GetServiceByIdRequest(id));
        return service.Succeeded ? Ok(service) : BadRequest(service);
    }

    [HttpPut("{id}")]
    [RequirePermission(Permissions.UpdateServices)]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> UpdateServiceById(Guid id, UpdateServiceDto updateService)
    {
        var service = await _mediator.Send(new UpdateServiceByIdRequest(id, updateService));
        return service.Succeeded ? Ok(service) : BadRequest(service);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteServices)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteServiceById(Guid id)
    {
        var service = await _mediator.Send(new DeleteServiceByIdRequest(id));
        return service.Succeeded ? Ok(service) : BadRequest(service);
    }
}
