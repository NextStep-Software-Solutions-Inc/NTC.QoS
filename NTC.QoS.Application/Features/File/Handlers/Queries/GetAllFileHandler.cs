using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.File.Handlers.Queries;

public class GetAllFileHandler(IFileRepository repository) : IRequestHandler<GetAllFilesRequest, PaginatedResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;
    public async Task<PaginatedResponse<FileDto>> Handle(GetAllFilesRequest request, CancellationToken cancellationToken)
    {
        var allFiles = await _repository.ListAsync(f => f.IsDeleted != true, cancellationToken);

        var fileDtos = allFiles.Select(file => new FileDto
        {
            Id = file.Id,
            Name = file.Name,
            Path = file.Path,
            UploadedBy = file.UploadedBy,
            UploadDate = file.UploadDate,
            Type = file.Type.ToString() ?? string.Empty,
            Status = file.Status.ToString() ?? string.Empty
        }).ToList();

        if (!string.IsNullOrEmpty(request.Search))
        {
            fileDtos = fileDtos.Where(f => f.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (fileDtos.Count is 0)
        {
            return new PaginatedResponse<FileDto>(fileDtos, request.PageNumber, request.PageSize)
            {
                Data = fileDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<FileDto>(fileDtos, request.PageNumber, request.PageSize)
        {
            Data = fileDtos,
            Message = "Files retrieved successfully.",
            Succeeded = true
        };
    }
}
