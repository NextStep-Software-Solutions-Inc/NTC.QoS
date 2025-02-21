using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("service-providers", Name = "service-providers")]
public class ServiceProvidersController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServiceProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ServiceProviderDto>>> GetAllServiceProviders(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllServiceProvidersRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return Ok(result);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ServiceProviderDto>>> GetServiceProviderById(Guid id)
    {
        Guid serviceProviderId = id;
        var request = new GetServiceProviderByIdRequest(serviceProviderId);
        var serviceProviders = await _mediator.Send(request);
        return serviceProviders.Succeeded ? Ok(serviceProviders) : BadRequest(serviceProviders);
    }
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Aggregates.ServiceProvider>>> UpdateServiceProviderById(Guid id, UpdateServiceProviderDto updateServiceProvider)
    {
        var serviceProvider = await _mediator.Send(new UpdateServiceProviderByIdRequest(id, new UpdateServiceProviderDto
        {
            MNC = updateServiceProvider.MNC,
            MCC= updateServiceProvider.MCC,
        }));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteServiceProviderById(Guid id)
    {
        var serviceProvider = await _mediator.Send(new DeleteServiceProviderByIdRequest(id));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }
    [HttpPost]
    public async Task<IActionResult> Post(CreateServiceProviderDto serviceProviderDto)
    {
        var serviceProvider = await _mediator.Send(new CreateServiceProviderRequest(serviceProviderDto));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }
}