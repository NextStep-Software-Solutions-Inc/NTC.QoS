using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.ConnectivityGap.Requests;

/// <summary>
/// Query to retrieve connectivity gap report as JSON data
/// </summary>
public class GetConnectivityGapReportQuery : IRequest<ApiResponse<ConnectivityGapReportDto>>
{
    /// <summary>
    /// Optional filter by Region ID
    /// </summary>
    public Guid? RegionId { get; set; }

    /// <summary>
    /// Optional filter by Province ID
    /// </summary>
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// Optional filter by Municipality ID
    /// </summary>
    public Guid? MunicipalityId { get; set; }

    /// <summary>
    /// Optional filter by connectivity status within gap areas.
    /// When null, all gap areas (Unserved and Underserved) are included.
    /// Only Unserved (1) and Underserved (2) are applicable; Served areas are never included in the gap report.
    /// </summary>
    public ConnectivityStatusEnum? FilterByStatus { get; set; }

    /// <summary>
    /// Page number for pagination (default: 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size for pagination (default: 10, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 10;
}
