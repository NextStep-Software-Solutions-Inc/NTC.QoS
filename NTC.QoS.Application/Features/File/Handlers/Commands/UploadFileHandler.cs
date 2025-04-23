using MediatR;
using NTC.QoS.Application.Contracts;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class UploadFileHandler(IFileRepository repository) : IRequestHandler<UploadFileRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;

    public async Task<ApiResponse<FileDto>> Handle(UploadFileRequest request, CancellationToken cancellationToken)
    {
        var fileType = EnumHelper<FileTypeEnum>.GetValue(request.File.Type);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string originalFileName = request.File.FileName ?? request.File.File.FileName;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName) ?? "UnknownFile";
        string fileExtension = Path.GetExtension(originalFileName);
        string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}".Replace(" ", "-");

        // Define the save path
        string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", request.File.Type);
        string filePath = Path.Combine(uploadDirectory, newFileName);

        // Ensure the directory exists
        if (!Directory.Exists(uploadDirectory))
        {
            Directory.CreateDirectory(uploadDirectory);
        }

        // Save the file
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.File.CopyToAsync(fileStream, cancellationToken);
        }

        var newFile = new Domain.Entities.File
        {
            Name = newFileName,
            Type = fileType,
            Path = filePath,
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
    }
}
