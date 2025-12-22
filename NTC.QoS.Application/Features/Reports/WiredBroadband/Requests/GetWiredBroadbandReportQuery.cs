using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;

public class GetWiredBroadbandReportQuery : IRequest<ApiResponse<WiredBroadbandReportDto>>
{
    public Guid? RegionId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? MunicipalityId { get; set; }
    
    /// <summary>
    /// Filter by connectivity status. When null, includes all statuses.
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
    
    /// <summary>
    /// When true, includes the Remarks column in Excel/PDF reports.
    /// When false (default), excludes the Remarks column to save space.
    /// </summary>
    public bool IncludeRemarks { get; set; } = true;
}