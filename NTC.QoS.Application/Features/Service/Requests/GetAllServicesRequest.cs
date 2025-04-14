using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class GetAllServicesRequest(int pageSize, int pageNumber, List<Guid>? providerIds, string? type) : IRequest<PaginatedResponse<ServiceDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public List<Guid>? ProviderIds { get; set; } = providerIds;
    public string? Type { get; set; } = type;
} 
