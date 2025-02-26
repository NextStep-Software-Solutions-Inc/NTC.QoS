
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("services", Name = "services")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ServiceDto>>> GetAllServices(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllServicesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return Ok(result);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> GetServiceById(Guid id)
    {
        Guid serviceId = id;
        var request = new GetServiceByIdRequest(serviceId);
        var services = await _mediator.Send(request);
        return services.Succeeded ? Ok(services) : BadRequest(services);
    }
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Service>>> UpdateServiceById(Guid id, UpdateServiceDto updateService)
    {
        var service = await _mediator.Send(new UpdateServiceByIdRequest(id, new UpdateServiceDto
        {
            Name = updateService.Name,
            Type = updateService.Type,
        }));
        return service.Succeeded ? Ok(service) : BadRequest(service);
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteServiceById(Guid id)
    {
        var service = await _mediator.Send(new DeleteServiceByIdRequest(id));
        return service.Succeeded ? Ok(service) : BadRequest(service);
    }
    [HttpPost]
    public async Task<IActionResult> Post(CreateServiceDto serviceDto)
    {
        var service = await _mediator.Send(new CreateServiceRequest(serviceDto));
        return service.Succeeded ? Ok(service) : BadRequest(service);
    }
}
