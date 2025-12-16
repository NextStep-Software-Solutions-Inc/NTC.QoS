using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("service-providers", Name = "service-providers")]
[Authorize]

public class ServiceProvidersController(IMediator mediator) : ControllerBase
{
    private const int BrowserCacheDuration = 21600;
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.CreateServiceProviders)]
    public async Task<ActionResult<ApiResponse<ServiceProviderDto>>> CreateServiceProvider(CreateServiceProviderDto createServiceProvider)
    {
        var serviceProvider = await _mediator.Send(new CreateServiceProviderRequest(createServiceProvider));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewServiceProviders)]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "providerIds", "serviceIds", "serviceTypes" })]
    public async Task<ActionResult<PaginatedResponse<ServiceProviderDto>>> GetAllServiceProviders(int pageSize = 10,
                                                                                                  int pageNumber = 1,
                                                                                                  [FromQuery] List<Guid>? providerIds = null,
                                                                                                  [FromQuery] List<Guid>? serviceIds = null,
                                                                                                  [FromQuery] List<string>? serviceTypes = null)
    {
        var serviceProviders = await _mediator.Send(new GetAllServiceProvidersRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            ServiceTypes = serviceTypes
        });
        return serviceProviders.Succeeded ? Ok(serviceProviders) : BadRequest(serviceProviders);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewServiceProviders)]
    public async Task<ActionResult<ApiResponse<ServiceProviderDto>>> GetServiceProviderById(Guid id)
    {
        var serviceProvider = await _mediator.Send(new GetServiceProviderByIdRequest(id));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }

    [HttpPut("{id}")]
    [RequirePermission(Permissions.UpdateServiceProviders)]
    public async Task<ActionResult<ApiResponse<ServiceProviderDto>>> UpdateServiceProviderById(Guid id, UpdateServiceProviderDto updateServiceProvider)
    {
        var serviceProvider = await _mediator.Send(new UpdateServiceProviderByIdRequest(id, updateServiceProvider));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteServiceProviders)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteServiceProviderById(Guid id)
    {
        var serviceProvider = await _mediator.Send(new DeleteServiceProviderByIdRequest(id));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }
}
