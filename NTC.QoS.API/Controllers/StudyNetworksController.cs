using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("study-networks", Name = "study-networks")]
public class StudyNetworksController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudyNetworkDto>>> CreateSector(CreateStudyNetworkDto createStudyNetwork)
    {
        var studyNetwork = await _mediator.Send(new CreateStudyNetworkRequest(createStudyNetwork));
        return studyNetwork.Succeeded ? Ok(studyNetwork) : BadRequest(studyNetwork);
    }
}
