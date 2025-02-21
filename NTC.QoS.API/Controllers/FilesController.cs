namespace NTC.QoS.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.File;
using NTC.QoS.Domain.Models.DTOs;

[ApiController]
[Route("files", Name = "files")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    public FilesController(IMediator mediator)
    {
        _mediator = mediator;
    }
}
