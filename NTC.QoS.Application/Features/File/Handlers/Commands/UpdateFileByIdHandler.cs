using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class UpdateFileByIdHandler(IFileRepository repository) : IRequestHandler<UpdateFileByIdRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;
    public async Task<ApiResponse<FileDto>> Handle(UpdateFileByIdRequest request, CancellationToken cancellationToken)
    {
        var fileQuery = _repository.Query(f => f.Id == request.Id && !f.IsDeleted);
        var file = await _repository.FirstOrDefaultAsync(fileQuery, cancellationToken);

        if (file is null)
        {
            return new ApiResponse<FileDto>
            {
                Message = "File not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["File with the specified ID does not exist."]
                    }
                }
            };
        }

        file.Name = request.File.Name;
        _repository.Update(file);

        await _repository.SaveChangesAsync();
        return new ApiResponse<FileDto>
        {
            Data = new FileDto
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                UploadedBy = file.UploadedBy,
                UploadDate = file.UploadDate,
                Type = file.Type.ToString() ?? string.Empty,
                Status = file.Status.ToString() ?? string.Empty
            },
            Message = "File updated successfully.",
            Succeeded = true
        };
    }
}
