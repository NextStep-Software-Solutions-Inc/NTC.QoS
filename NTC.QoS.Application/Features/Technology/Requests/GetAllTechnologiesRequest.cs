using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;
namespace NTC.QoS.Application.Features.Technology.Requests;

public class GetAllTechnologiesRequest : IRequest<PaginatedResponse<TechnologyDto>>
{
    public GetAllTechnologiesRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllTechnologiesRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? GenerationId { get; set; }
}


