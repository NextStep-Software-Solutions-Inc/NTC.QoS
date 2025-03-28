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
        string fileName = ((request.File.FileName ?? request.File.File.FileName.Split(".").First()) + "_" +timestamp).Replace(" ", "-");

        var newFile = new Domain.Entities.File
        {
            Name = fileName,
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
            Message = "File created successfully",
            Succeeded = true
        };
    }
}