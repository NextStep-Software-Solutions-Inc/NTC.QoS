using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class CreateFileHandler(IRepository<Domain.Entities.File> repository) : IRequestHandler<CreateFileRequest, ApiResponse<CreateFileDto>>
{
    private readonly IRepository<Domain.Entities.File> _repository = repository;
    public async Task<ApiResponse<CreateFileDto>> Handle(CreateFileRequest request, CancellationToken cancellationToken)
    {
        var file = await _repository.FindAsync(f => f.Name == request.Name && !f.IsDeleted, cancellationToken);
        if (file != null)
        {
            return new ApiResponse<CreateFileDto>
            {
                Message = "File already exists",
                Succeeded = false
            };
        }
        var newFile = new Domain.Entities.File
        {
            Name = request.Name,
            Path = $"files/{request.Name}",
            Type = EnumHelper<FileTypeEnum>.GetValue(request.Type)

        };
        _repository.Add(newFile);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<CreateFileDto>
        {
            Data = new CreateFileDto
            {
                Name = newFile.Name,
                Path = newFile.Path,
                Type = EnumHelper<FileTypeEnum>.GetName(newFile.Type)
            },
            Message = "File created successfully",
            Succeeded = true
        };
    }
}