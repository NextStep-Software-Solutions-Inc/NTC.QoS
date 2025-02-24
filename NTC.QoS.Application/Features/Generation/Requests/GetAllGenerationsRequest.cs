using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Requests;

public class GetAllGenerationsRequest : IRequest<PaginatedResponse<GenerationDto>>
{
    public GetAllGenerationsRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }

    public GetAllGenerationsRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}