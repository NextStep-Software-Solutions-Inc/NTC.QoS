using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Queries;

public class GetAllFileHandler(IFileRepository repository) : IRequestHandler<GetAllFilesRequest, PaginatedResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;

    public async Task<PaginatedResponse<FileDto>> Handle(GetAllFilesRequest request, CancellationToken cancellationToken)
    {
        var filesQuery = _repository.QueryAsNoTracking();

        if (!string.IsNullOrEmpty(request.Search))
        {
            filesQuery = _repository.SearchByName(filesQuery, request.Search);
        }

        filesQuery = filesQuery
            .Where(f => request.Types == null || request.Types.Length == 0 || request.Types.Contains(f.Type.ToString()))
            .Where(f => request.Statuses == null || request.Statuses.Length == 0 || request.Statuses.Contains(f.Status.ToString()))
            .Where(f => request.RegionIds == null || request.RegionIds.Length == 0 || (f.RegionId != null && request.RegionIds.Contains(f.RegionId.Value)));

        var filesDtoQuery = filesQuery
            .OrderByDescending(file => file.UploadDate)
            .Select(file => new FileDto
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                UploadedBy = file.UploadedBy,
                UploadDate = file.UploadDate,
                Type = file.Type.ToString() ?? string.Empty,
                Status = file.Status.ToString() ?? string.Empty,
                RegionId = file.RegionId
            });

        return await _repository.PaginateAsync(filesDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
