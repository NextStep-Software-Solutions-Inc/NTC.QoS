using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("study-networks", Name = "study-networks")]
[Authorize]

public class StudyNetworksController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudyNetworkDto>>> CreateSector(CreateStudyNetworkDto createStudyNetwork)
    {
        var studyNetwork = await _mediator.Send(new CreateStudyNetworkRequest(createStudyNetwork));
        return studyNetwork.Succeeded ? Ok(studyNetwork) : BadRequest(studyNetwork);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<StudyNetworkDto>>> GetAllStudyNetworks(int pageSize = 10,
                                                                                            int pageNumber = 1,
                                                                                            [FromQuery] List<Guid>? studyIds = null,
                                                                                            [FromQuery] List<Guid>? networkIds = null)
    {
        var studyNetworks = await _mediator.Send(new GetAllStudyNetworksRequest(pageSize, pageNumber, studyIds, networkIds));
        return studyNetworks.Succeeded ? Ok(studyNetworks) : NotFound(studyNetworks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudyNetworkDto>>> GetStudyNetworkById(Guid id)
    {
        var studyNetwork = await _mediator.Send(new GetStudyNetworkByIdRequest(id));
        return studyNetwork.Succeeded ? Ok(studyNetwork) : NotFound(studyNetwork);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteStudyNetworkById(Guid id)
    {
        var studyNetwork = await _mediator.Send(new DeleteStudyNetworkByIdRequest(id));
        return studyNetwork.Succeeded ? Ok(studyNetwork) : NotFound(studyNetwork);
    }
}
