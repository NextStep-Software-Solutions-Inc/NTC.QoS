using MediatR;
using NTC.QoS.Application.Contracts;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
using System.Text.Json;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class UploadFileHandler(IFileRepository repository, IEnumerable<IFileProcessor> fileProcessors) : IRequestHandler<UploadFileRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;
    private readonly IEnumerable<IFileProcessor> _fileProcessors = fileProcessors;

    public async Task<ApiResponse<FileDto>> Handle(UploadFileRequest request, CancellationToken cancellationToken)
    {
        var fileType = EnumHelper<FileTypeEnum>.GetValue(request.File.Type);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string originalFileName = request.File.FileName ?? request.File.File.FileName;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName) ?? "UnknownFile";
        string fileExtension = Path.GetExtension(originalFileName);
        string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}".Replace(" ", "-");

        using var memoryStream = new MemoryStream();
        await request.File.File.CopyToAsync(memoryStream, cancellationToken);
        byte[] fileContent = memoryStream.ToArray();
        string contentType = request.File.File.ContentType;

        var newFile = new Domain.Entities.File
        {
            Name = newFileName,
            Type = fileType,
            Path = Path.Combine("uploads", newFileName),
            Status = FileStatusEnum.Pending,
        };
        _repository.Add(newFile);
        await _repository.SaveChangesAsync(cancellationToken);

        var newFileDto = new FileDto
        {
            Id = newFile.Id,
            Name = newFile.Name,
            Path = newFile.Path,
            UploadedBy = newFile.UploadedBy,
            UploadDate = newFile.UploadDate,
            Type = newFile.Type.ToString() ?? string.Empty,
            Status = newFile.Status.ToString()
        };

        return new ApiResponse<FileDto>
        {
            Data = newFileDto,
            Message = "File uploaded and queued for processing.",
            Succeeded = true
        };

        //var processor = _fileProcessors.FirstOrDefault(p => p.FileType == fileType);
        //if (processor is null)
        //{
        //    newFile.Status = FileStatusEnum.Failed;
        //    await _repository.SaveChangesAsync(cancellationToken);

        //    return new ApiResponse<FileDto>
        //    {
        //        Succeeded = false,
        //        Message = $"Unsupported file type: {fileType}. No processor available.",
        //        Errors = new Dictionary<string, string[]>
        //        {
        //            {
        //                "NullProcessor", [$"File upload failed. Unsupported file type: {fileType}. No processor available."]
        //            }
        //        }
        //    };
        //}

        //newFile.Status = FileStatusEnum.Processing;
        //await _repository.SaveChangesAsync(cancellationToken);

        //var processingResultWrapper = await processor.ProcessFileAsync(newFile);

        //if (!processingResultWrapper.Succeeded)
        //{
        //    newFile.Status = FileStatusEnum.PartiallyProcessed;
        //    await _repository.SaveChangesAsync(cancellationToken);

        //    var newFileDto = new FileDto
        //    {
        //        Id = newFile.Id,
        //        Name = newFile.Name,
        //        Path = newFile.Path,
        //        UploadedBy = newFile.UploadedBy,
        //        UploadDate = newFile.UploadDate,
        //        Type = newFile.Type.ToString() ?? string.Empty,
        //        Status = newFile.Status.ToString()
        //    };

        //    var errors = new Dictionary<string, string[]>();

        //    if (processingResultWrapper.Data?.Data is Dictionary<string, string[]> dataErrors)
        //    {
        //        errors = dataErrors;
        //    }
        //    else if (processingResultWrapper.Data?.Data is List<object> errorList)
        //    {
        //        var errorMessages = errorList
        //            .Select(e => JsonSerializer.Serialize(e))
        //            .ToArray();
        //        errors.Add("ProcessingError", errorMessages);
        //    }
        //    else if (!string.IsNullOrEmpty(processingResultWrapper.Message))
        //    {
        //        errors.Add("ProcessingError", new[] { processingResultWrapper.Message });
        //    }

        //    return new ApiResponse<FileDto>
        //    {
        //        Succeeded = true,
        //        Message = $"File '{newFile.Name}' upload successful, but processing failed: {processingResultWrapper.Message}",
        //        Data = newFileDto,
        //        Errors = errors
        //    };
        //}
        //else
        //{
        //    newFile.Status = FileStatusEnum.Success;
        //    await _repository.SaveChangesAsync(cancellationToken);

        //    var newFileDto = new FileDto
        //    {
        //        Id = newFile.Id,
        //        Name = newFile.Name,
        //        Path = newFile.Path,
        //        UploadedBy = newFile.UploadedBy,
        //        UploadDate = newFile.UploadDate,
        //        Type = newFile.Type.ToString() ?? string.Empty,
        //        Status = newFile.Status.ToString()
        //    };

        //    return new ApiResponse<FileDto>
        //    {
        //        Data = newFileDto,
        //        Message = "File uploaded and processed successfully.",
        //        Succeeded = true
        //    };
        //}
    }
}
