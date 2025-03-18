using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Requests;

public class GetAllOrganizationsRequest(int pageSize, int pageNumber) : IRequest<PaginatedResponse<GetOrganizationDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
}
