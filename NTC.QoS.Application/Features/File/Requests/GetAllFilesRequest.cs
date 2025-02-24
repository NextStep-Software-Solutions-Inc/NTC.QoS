using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public class GetAllFilesRequest : IRequest<PaginatedResponse<FileDto>>
{
    public GetAllFilesRequest(int pageNumber, int pageSize, string? search)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        Search = search;
    }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Search { get; set; }
}
