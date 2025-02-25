using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Queries;

public class GetAllFileHandler(IRepository<Domain.Entities.File> repository) : IRequestHandler<GetAllFilesRequest, PaginatedResponse<FileDto>>
{
    private readonly IRepository<Domain.Entities.File> _repository = repository;
    public async Task<PaginatedResponse<FileDto>> Handle(GetAllFilesRequest request, CancellationToken cancellationToken)
    {
        var allFiles = await _repository.ListAsync(f => f.IsDeleted != true, cancellationToken);
        var result = allFiles.Select(file => new FileDto
        {
            Id = file.Id,
            Name = file.Name,
            Path = file.Path
        }).ToList();

        if (!string.IsNullOrEmpty(request.Search))
        {
            result = result.Where(f => f.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return new PaginatedResponse<FileDto>(result, request.PageNumber, request.PageSize);
    }
}
