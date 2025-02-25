using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Requests;

public class GetAllAreaStudiesRequest : IRequest<PaginatedResponse<AreaStudyDto>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Search { get; set; }
    public GetAllAreaStudiesRequest(int pageNumber, int pageSize, string? search)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        Search = search;
    }
}
