using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Requests;

public class GetAllMunicipalitiesRequest : IRequest<PaginatedResponse<MunicipalityDto>>
{
    public GetAllMunicipalitiesRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllMunicipalitiesRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Filter by multiple province IDs. Takes precedence over ProvinceId.
    /// </summary>
    public Guid[]? ProvinceIds { get; set; }
    
    /// <summary>
    /// [Deprecated] Filter by a single province ID. Use ProvinceIds instead for better flexibility.
    /// Note: This will be ignored if ProvinceIds is provided.
    /// </summary>
    [Obsolete("Use ProvinceIds instead for filtering by multiple provinces. This property will be removed in a future version.")]
    public Guid? ProvinceId { get; set; }
    
    public Guid[]? RegionIds { get; set; }
    public bool? IsWiredCovered { get; set; }
    public bool? IsWirelessCovered { get; set; }
}
