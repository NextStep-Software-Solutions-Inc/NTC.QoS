using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("sectors", Name= "sectors")]
public class SectorsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<PaginatedResponse<SectorDto>>> CreateSector(CreateSectorDto sectorDto)
    {
        var sector = await _mediator.Send(new CreateSectorRequest(sectorDto));
        return sector.Succeeded ? Ok(sector) : BadRequest(sector);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SectorDto>>> GetAllSectors(int pageSize = 10,
                                                int pageNumber = 1,
                                                Guid? networkId = null,
                                                Guid? mimoId = null,
                                                Guid? cellId = null,
                                                Guid? txAntennaPatternId = null,
                                                Guid? rxAntennaPatternId = null)
    {
        var sectors = await _mediator.Send(new GetAllSectorsRequest(pageSize,
                                                                    pageNumber,
                                                                    networkId,
                                                                    mimoId,
                                                                    cellId,
                                                                    txAntennaPatternId,
                                                                    rxAntennaPatternId));
       
        return sectors.Succeeded ? Ok(sectors) : BadRequest(sectors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SectorDto>>> GetSectorById(Guid id)
    {
        var sector = await _mediator.Send(new GetSectorByIdRequest(id));
        return sector.Succeeded ? Ok(sector) : BadRequest(sector);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<SectorDto>>> UpdateSector(Guid id, UpdateSectorDto sectorDto)
    {
        var sector = await _mediator.Send(new UpdateSectorByIdRequest(id, sectorDto));
        return sector.Succeeded ? Ok(sector) : BadRequest(sector);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSector(Guid id)
    {
        var sector = await _mediator.Send(new DeleteSectorByIdRequest(id));
        return sector.Succeeded ? Ok(sector) : BadRequest(sector);
    }
}
