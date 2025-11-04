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

        if (!string.IsNullOrEmpty(request.Search))
        {
            // Note: SearchByName operates on entity query, not DTO query
            // We need to apply search before projection
            filesQuery = _repository.SearchByName(filesQuery, request.Search);
            
            // Re-project after filtering
            filesDtoQuery = filesQuery.Select(file => new FileDto
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                UploadedBy = file.UploadedBy,
                UploadDate = file.UploadDate,
                Type = file.Type.ToString() ?? string.Empty,
                Status = file.Status.ToString() ?? string.Empty
            });
        }

        // Apply enum filters on DTO query (these are already converted to strings)
        filesDtoQuery = filesDtoQuery
            .Where(f => request.Types == null || request.Types.Length == 0 || request.Types.Contains(f.Type))
            .Where(f => request.Statuses == null || request.Statuses.Length == 0 || request.Statuses.Contains(f.Status));

        return await _repository.PaginateAsync(filesDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
