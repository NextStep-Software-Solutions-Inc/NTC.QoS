using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.NAP;
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
        var file = await _mediator.Send(new CreateFileRequest(createFile));

        return file.Succeeded ? Ok(file) : BadRequest(file);
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<FileDto>>> GetAllFiles(int pageSize = 10, int pageNumber = 1, string? search = null)
    {
        var file = await _mediator.Send(new GetAllFilesRequest(pageNumber, pageSize, search));
        return file.Succeeded ? Ok(file) : BadRequest(file);
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
    public async Task<ActionResult<ApiResponse<FileDto>>>
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