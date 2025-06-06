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
        var filesQuery = _repository.Query(f => f.IsDeleted != true);

        var filesDtoQuery = filesQuery.Select(file => new FileDto
        {
            Id = file.Id,
            Name = file.Name,
            Path = file.Path,
            UploadedBy = file.UploadedBy,
            UploadDate = file.UploadDate,
            Type = file.Type.ToString() ?? string.Empty,
            Status = file.Status.ToString() ?? string.Empty
        });

        filesDtoQuery = filesDtoQuery
            .Where(f => string.IsNullOrEmpty(request.Search) || f.Name.Contains(request.Search)) 
            .Where(f => request.Types == null || request.Types.Length == 0 || request.Types.Contains(f.Type))
            .Where(f => request.Statuses == null || request.Statuses.Length == 0 || request.Statuses.Contains(f.Status));

        return await _repository.PaginateAsync(filesDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
