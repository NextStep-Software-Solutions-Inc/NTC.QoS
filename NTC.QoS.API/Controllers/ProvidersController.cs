using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("providers", Name = "providers")]
[Authorize]

public class ProvidersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> CreateProvider([FromBody] CreateProviderDto createProvider)
    {
        var provider = await _mediator.Send(new CreateProviderRequest(createProvider));
        return provider.Succeeded ? Ok(provider) : BadRequest(provider);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProviderDto>>> GetAllProviders(int pageSize = 10, int pageNumber = 1, [FromQuery] List<Guid>? serviceIds = null, [FromQuery] List<string>? names = null)
    {
        var providers = await _mediator.Send(new GetAllProvidersRequest(pageSize, pageNumber, serviceIds, names));
        return providers.Succeeded ? Ok(providers) : BadRequest(providers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> GetProviderById(Guid id)
    {
        var providers = await _mediator.Send(new GetProviderByIdRequest(id));
        return providers.Succeeded ? Ok(providers) : BadRequest(providers);
    }

    [HttpGet("{id}/logo")]
    public async Task<IActionResult> GetProviderLogo(Guid id)
    {
        var result = await _mediator.Send(new GetProviderLogoByIdRequest(id));
        
        if (!result.Succeeded || result.Data == null)
        {
            return NotFound(result);
        }

        return File(result.Data.LogoData, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("{id}/logo")]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> AddProviderLogo(Guid id, [FromForm] UploadProviderLogoDto logo)
    {
        var result = await _mediator.Send(new AddProviderLogoByIdRequest(id, logo));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}/logo")]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> DeleteProviderLogo(Guid id)
    {
        var result = await _mediator.Send(new DeleteProviderLogoByIdRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> UpdateProviderById(Guid id, UpdateProviderDto updateProvider)
    {
        var provider = await _mediator.Send(new UpdateProviderByIdRequest(id, new UpdateProviderDto
        {
            Name = updateProvider.Name,
            Description = updateProvider.Description,
            LicenseNumber = updateProvider.LicenseNumber,
            Color = updateProvider.Color
        }));
        return provider.Succeeded ? Ok(provider) : BadRequest(provider);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProviderById(Guid id)
    {
        var provider = await _mediator.Send(new DeleteProviderByIdRequest(id));
        return provider.Succeeded ? Ok(provider) : BadRequest(provider);
    }
}
