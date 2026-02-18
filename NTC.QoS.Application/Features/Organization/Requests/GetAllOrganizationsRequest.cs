using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Requests;

public class GetAllOrganizationsRequest(int pageSize, int pageNumber, string? search = null) : IRequest<PaginatedResponse<OrganizationDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public string? Search { get; set; } = search;
}
