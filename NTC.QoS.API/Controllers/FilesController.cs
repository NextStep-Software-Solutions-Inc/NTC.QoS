using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("files", Name = "files")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    public FilesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<FileDto>>> CreateFile(CreateFileDto createFile)
    {
        var result = await _mediator.Send(new CreateFileRequest
        {
            Name = createFile.Name,
            Path = createFile.Path,
            Type = createFile.Type
        });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Domain.Entities.File>>> GetAllFiles(int pageSize = 10, int pageNumber = 1, string? search = null)
    {
        var result = await _mediator.Send(new GetAllFilesRequest(pageNumber, pageSize, search));
        return Ok(result);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FileDto>>> GetFileById(Guid id)
    {
        Guid fileId = id;
        var request = new GetFileByIdRequest(fileId);
        var files = await _mediator.Send(request);
        return files.Succeeded ? Ok(files) : BadRequest(files);
    }
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.File>>>
        UpdateFileById(Guid id, UpdateFileDto updateFile)
    {
        var file = await _mediator.Send(new UpdateFileByIdRequest(id, new UpdateFileDto
        {
            Name = updateFile.Name
        }));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>>
        DeleteFileById(Guid id)
    {
        var result = await _mediator.Send(new DeleteFileByIdRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}