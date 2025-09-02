using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("files", Name = "files")]
[Authorize]

public class FilesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.UploadFiles)]
    public async Task<ActionResult<ApiResponse<FileDto>>> UploadFile( [FromForm] UploadFileDto uploadFile)
    {
        var file = await _mediator.Send(new UploadFileRequest(uploadFile));

        if (file.Succeeded.Equals(false) || file == null || file.Data == null)
        {
            return BadRequest(new ApiResponse<FileDto>
            {
                Succeeded = false,
                Message = "File upload failed."
            });
        }

        var fileType = Enum.Parse<FileTypeEnum>(uploadFile.Type);

        switch (fileType)
        {
            case FileTypeEnum.NAP:
                // Process NAP file
                await _mediator.Send(new ProcessNAPFileRequest(file.Data));
                break;
            case FileTypeEnum.Speedtest:
                // Process Speedtest file
                await _mediator.Send(new ProcessSpeedtestFileRequest(file.Data));
                break;

            case FileTypeEnum.Cell:
                await _mediator.Send(new ProcessCellFileRequest(file.Data));
                break;

            case FileTypeEnum.CellularCoverage:
                await _mediator.Send(new ProcessCellularCoverageFileRequest(file.Data));
                break;

            case FileTypeEnum.Unknown:
                default:
                throw new ArgumentOutOfRangeException(nameof(fileType), $"Unsupported file type: {fileType}");
        }

        // Schedule a background job to delete the file and its database record after 1 day
        return Ok(file);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewFiles)]
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
    [RequirePermission(Permissions.ViewFiles)]
    public async Task<ActionResult<ApiResponse<FileDto>>> GetFileById(Guid id)
    {
        var file = await _mediator.Send(new GetFileByIdRequest(id));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }

    [HttpPut("{id}")]
    [RequirePermission(Permissions.UpdateFiles)]
    public async Task<ActionResult<ApiResponse<FileDto>>> UpdateFileById(Guid id, UpdateFileDto updateFile)
    {
        var file = await _mediator.Send(new UpdateFileByIdRequest(id, new UpdateFileDto
        {
            Name = updateFile.Name
        }));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteFiles)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFileById(Guid id)
    {
        var file = await _mediator.Send(new DeleteFileByIdRequest(id));
        return file.Succeeded ? Ok(file) : BadRequest(file);
    }

    [HttpGet("download/{type}")]
    [RequirePermission(Permissions.DownloadFiles)]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Stream>>> DownloadFile(string type)
    {
        var res = await _mediator.Send(new DownloadTemplateFileRequest(type));
        if(res.Succeeded is false || res.Data is null || res.Data.Length is 0)
        {
            return BadRequest(res);
        }
        return File(res.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{type}_template-file.xlsx");
    }

}
