using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
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
    public async Task<ActionResult<ApiResponse<FileDto>>> UploadFile([FromForm] UploadFileDto uploadFile)
    {
        var file = await _mediator.Send(new UploadFileRequest(uploadFile));

        if (file.Succeeded.Equals(false) || file == null || file.Data == null)
        {
            return BadRequest(file);
        }

        FileTypeEnum fileType = Enum.TryParse<FileTypeEnum>(uploadFile.Type, out var parsedType) ? parsedType : FileTypeEnum.Unknown;

        switch (fileType)
        {
            case FileTypeEnum.NAP:
                await _mediator.Send(new ProcessNAPFileRequest(file.Data));
                break;
            case FileTypeEnum.Speedtest:
                await _mediator.Send(new ProcessSpeedtestFileRequest(file.Data));
                break;

            case FileTypeEnum.Cell:
                await _mediator.Send(new ProcessCellFileRequest(file.Data));
                break;

            case FileTypeEnum.CellularCoverage:
                await _mediator.Send(new ProcessCellularCoverageFileRequest(file.Data));
                break;

            case FileTypeEnum.WirelessValidation:
                await _mediator.Send(new ProcessWirelessValidationFileRequest(file.Data));
                break;

            case FileTypeEnum.Unknown:
            default:
                throw new InvalidEnumArgumentException();
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
                                                                            [FromQuery] Guid[]? regionIds = default,
                                                                            string? search = default)
    {
        var files = await _mediator.Send(new GetAllFilesRequest(pageNumber, pageSize, search)
        {
            Types = types,
            Statuses = statuses,
            RegionIds = regionIds
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

    [HttpGet("presigned-upload-url")]
    [RequirePermission(Permissions.UploadFiles)]
    public async Task<ActionResult<ApiResponse<PreSignedUrlDto>>> GetPreSignedUploadUrl(
        [FromQuery] string fileName,
        [FromQuery] string fileType,
        [FromQuery] Guid regionId)
    {
        var response = await _mediator.Send(new GetPreSignedUploadUrlRequest(fileName, fileType, regionId));
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id}/presigned-download-url")]
    [RequirePermission(Permissions.ViewFiles)]
    public async Task<ActionResult<ApiResponse<PreSignedUrlDto>>> GetPreSignedDownloadUrl(Guid id)
    {
        var response = await _mediator.Send(new GetPreSignedDownloadUrlRequest(id));
        return response.Succeeded ? Ok(response) : BadRequest(response);
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
        if (res.Succeeded is false || res.Data is null || res.Data.Length is 0)
        {
            return BadRequest(res);
        }
        return File(res.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{type}_template-file.xlsx");
    }

    [HttpGet("download/wireless-validation/{municipalityId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Stream>>> DownloadWirelessValidationTemplate(Guid municipalityId)
    {
        var res = await _mediator.Send(new DownloadWirelessValidationTemplateRequest(municipalityId));
        if (res.Succeeded is false || res.Data is null || res.Data.Length is 0)
        {
            return BadRequest(res);
        }

        var municipalityResponse = await _mediator.Send(new GetMunicipalityByIdRequest(municipalityId));

        string name = "unknown";
        if (municipalityResponse.Succeeded && municipalityResponse.Data is not null)
        {
            name = municipalityResponse.Data.Name?.Replace(" ", "_") ?? "unknown";
        }

        return File(res.Data,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"wireless_validation_template-{name}.xlsx");
    }

    /// <summary>
    /// Upload cellular coverage V2 CSV file with user-provided metadata.
    /// Expects CSV format: Lat, Lng, Signal, Address
    /// User provides: NetworkId, ServiceProviderId, MunicipalityId
    /// System derives: BandId and TechnologyId from Network
    /// </summary>
    /// <param name="request">Upload request containing the CSV file and metadata</param>
    /// <returns>Upload result with job ID for tracking</returns>
    [HttpPost("cellular-coverage-v2")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<ApiResponse<CellularCoverageV2UploadResultDto>>> UploadCellularCoverageV2(
        [FromForm] UploadCellularCoverageV2Request request)
    {
        var command = new UploadCellularCoverageV2Command
        {
            NetworkId = request.NetworkId,
            ServiceProviderId = request.ServiceProviderId,
            MunicipalityId = request.MunicipalityId,
            RegionId = request.RegionId,
            CsvFile = request.File
        };

        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

