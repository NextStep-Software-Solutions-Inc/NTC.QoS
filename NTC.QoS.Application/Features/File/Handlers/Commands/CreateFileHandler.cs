using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class CreateFileHandler(IFileRepository repository) : IRequestHandler<CreateFileRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;
    public async Task<ApiResponse<FileDto>> Handle(CreateFileRequest request, CancellationToken cancellationToken)
    {
        var file = await _repository.FindAsync(f => f.Name == request.File.Name, cancellationToken);

        if (file is not null)
        {
            return new ApiResponse<FileDto>
            {
                Message = "File name already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Name", ["File with the same name already exists."]
                    }
                }
            };
        }
        var newFile = new Domain.Entities.File
        {
            Name = request.File.Name,
            Path = $"files/{request.File.Name}",
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