using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

/// <summary>
/// Query to generate wireless broadband executive summary report with municipality and barangay breakdown.
/// Similar to wired broadband report but focuses on wireless coverage and connectivity.
/// </summary>
public class GetWirelessBroadbandExecutiveSummaryQuery : IRequest<ApiResponse<WirelessBroadbandExecutiveSummaryDto>>
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
    /// Optional filter by specific Municipality ID
    /// </summary>
    public Guid? MunicipalityId { get; set; }
    
    /// <summary>
    /// Filter by service status. When null, includes all statuses.
    /// When set, only includes barangays with the specified status.
    /// </summary>
    public ServiceStatusEnum? FilterByStatus { get; set; } = null;
    
    /// <summary>
    /// When true, forces generation of new connectivity assessments even if recent ones exist.
    /// When false (default), uses existing assessments from the last month if available.
    /// </summary>
    public bool ForceGenerateConnectivityAssessment { get; set; } = false;
    
    /// <summary>
    /// When true, disables pagination and returns all matching municipalities.
    /// Use this for Excel/PDF exports to ensure complete data.
    /// When false (default), uses PageNumber and PageSize for pagination.
    /// </summary>
    public bool DisablePagination { get; set; } = false;
    
    /// <summary>
    /// Page number for pagination (default: 1)
    /// Ignored when DisablePagination = true
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Page size for pagination (default: 10, max: 100)
    /// Ignored when DisablePagination = true
    /// </summary>
    public int PageSize { get; set; } = 10;
}
