using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Requests;

public class GetAllAreaStudiesRequest(int pageNumber, int pageSize, string? search) : IRequest<PaginatedResponse<AreaStudyDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public string? Search { get; set; } = search;
}
