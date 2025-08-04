using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Analysis.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analysis;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("analysis", Name = "analysis")]
[Authorize]
public class AnalysisController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("municipality/{municipalityId}/connectivity")]
    public async Task<ActionResult<ApiResponse<MunicipalityConnectivityAnalysisDto>>> GetMunicipalityConnectivityAnalysis(
        Guid municipalityId,
        [FromQuery] int timeWindowDays = 30,
        [FromQuery] decimal minDownloadSpeed = 25,
        [FromQuery] decimal minUploadSpeed = 3,
        [FromQuery] decimal maxLatency = 100,
        [FromQuery] int minTestSampleSize = 10)
    {
        var request = new GetMunicipalityConnectivityAnalysisRequest(
            municipalityId,
            TimeSpan.FromDays(timeWindowDays),
            minDownloadSpeed,
            minUploadSpeed,
            maxLatency,
            minTestSampleSize
        );

        var result = await _mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}