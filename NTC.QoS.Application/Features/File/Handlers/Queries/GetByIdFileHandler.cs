using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Queries;

public class GetByIdFileHandler(IFileRepository repository) : IRequestHandler<GetFileByIdRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;
    public async Task<ApiResponse<FileDto>> Handle(GetFileByIdRequest request, CancellationToken cancellationToken)
    {
        var fileQuery = _repository.Query(f => !f.IsDeleted && f.Id.Equals(request.Id));
        var file = await _repository.FirstOrDefaultAsync(fileQuery, cancellationToken);

        if (file is null)
        {
            return new ApiResponse<FileDto>
            {
                Data = null,
                Message = "File not found",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["File with the specified ID does not exist."]
                    }
                }
            };
        }
        return new ApiResponse<FileDto>
        {
            Message = "File data retrieved successfully.",
            Succeeded = true,
            Data = new FileDto
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                UploadedBy = file.UploadedBy,
                UploadDate = file.UploadDate,
                Type = file.Type.ToString() ?? string.Empty,
                Status = file.Status.ToString() ?? string.Empty,
                RegionId = file.RegionId,
                AssessmentJobId = file.AssessmentJobId,
                ErrorMessage = file.ErrorMessage,
                ErrorLogPath = file.ErrorLogPath
            }
        };
    }
}