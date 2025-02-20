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
    public async Task<IActionResult> GetAllProviders(int pageNumber = 1, int pageSize = 10)
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
