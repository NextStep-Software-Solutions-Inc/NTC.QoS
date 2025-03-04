using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Requests;

public class GetAllGenerationBandsRequest : IRequest<PaginatedResponse<GetGenerationBandDto>>
{
    public GetAllGenerationBandsRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }

    public GetAllGenerationBandsRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? BandId { get; set; }
}