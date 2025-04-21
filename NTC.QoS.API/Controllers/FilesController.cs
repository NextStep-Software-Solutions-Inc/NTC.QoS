using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
using System.IO;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("files", Name = "files")]
public class FilesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FileDto>>> UploadFile( [FromForm] UploadFileDto uploadFile)
    {
        var file = await _mediator.Send(new UploadFileRequest(uploadFile));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<FileDto>>> GetAllFiles(int pageSize = 10, 
                                                                            int pageNumber = 1, 
                                                                            [FromQuery] string[]? types = default, 
                                                                            [FromQuery] string[]? statuses = default,
                                                                            string? search = default)
    {
        var files = await _mediator.Send(new GetAllFilesRequest(pageNumber, pageSize, search)
        { 
            Types = types,
            Statuses = statuses
        });
        return files.Succeeded ? Ok(files) : BadRequest(files);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FileDto>>> GetFileById(Guid id)
    {
        var file = await _mediator.Send(new GetFileByIdRequest(id));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<FileDto>>> UpdateFileById(Guid id, UpdateFileDto updateFile)
    {
        var file = await _mediator.Send(new UpdateFileByIdRequest(id, new UpdateFileDto
        {
            Name = updateFile.Name
        }));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFileById(Guid id)
    {
        var file = await _mediator.Send(new DeleteFileByIdRequest(id));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }

    [HttpPost("download/{type}")]
    public async Task<ActionResult<ApiResponse<FileDto>>> DownloadFile(string type)
    {
        var stream = await _mediator.Send(new DownloadTemplateFileRequest(type));
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "file.xlsx");

    }
}
