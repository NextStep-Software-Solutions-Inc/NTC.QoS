using MediatR;
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
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string originalFileName = request.File.FileName ?? request.File.File.FileName;

        // Robust file name and extension extraction
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        string fileExtension = Path.GetExtension(originalFileName);

        // Ensure a default file name if extraction fails or is empty.
        fileNameWithoutExtension = string.IsNullOrEmpty(fileNameWithoutExtension) ? "UnknownFile" : fileNameWithoutExtension;

        // Construct the new file name with timestamp
        string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}".Replace(" ", "-");

        var newFile = new Domain.Entities.File
        {
            Name = newFileName,
            Type = EnumHelper<FileTypeEnum>.GetValue(request.File.Type)
        };

        _repository.Add(newFile);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<FileDto>
        {
            Data = new FileDto
            {
                Id = newFile.Id,
                Name = newFile.Name,
                Path = newFile.Path,
                UploadedBy = newFile.UploadedBy,
                UploadDate = newFile.UploadDate,
                Type = newFile.Type.ToString() ?? string.Empty,
                Status = newFile.Status.ToString() ?? string.Empty
            },
            Message = "File uploaded successfully.",
            Succeeded = true
        };
    }
}
