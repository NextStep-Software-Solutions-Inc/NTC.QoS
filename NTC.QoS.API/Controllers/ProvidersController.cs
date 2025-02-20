using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("providers", Name = "providers")]
public class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProviderDto>>> GetAllProviders(int pageSize = 10, int pageNumber = 1  )
    {
        var result = await _mediator.Send(new GetAllProvidersRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        return Ok(result);
    }
    [HttpGet("id")]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> GetProviderById(Guid id)
    {
        Guid providerId = id;
        var request = new GetProviderByIdRequest(providerId);
        var providers = await _mediator.Send(request);
        return providers.Succeeded ? Ok(providers) : BadRequest(providers);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Provider>>> UpdateProviderById(Guid id, UpdateProviderDto updateProvider)
    {
        var provider = await _mediator.Send(new UpdateProviderByIdRequest(id, new UpdateProviderDto
        {
            Name = updateProvider.Name,
            Description = updateProvider.Description,
            LicenseNumber = updateProvider.LicenseNumber
        }));

        return provider.Succeeded ? Ok(provider) : BadRequest(provider);
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Provider>>> DeleteProviderById(Guid id)
    {
        var provider = await _mediator.Send(new DeleteProviderByIdRequest(id));

        return provider.Succeeded ? Ok(provider) : BadRequest(provider);
    }

    [HttpPost]
    public async Task<IActionResult> Post(CreateProviderDto providerDto)
    {
        var provider = await _mediator.Send(new CreateProviderRequest
        {
            Provider = providerDto
        });

        return provider.Succeeded ? Ok(provider) : BadRequest(provider);
    }
}
