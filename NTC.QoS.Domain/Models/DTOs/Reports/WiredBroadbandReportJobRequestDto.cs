using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Request DTO for wired broadband report generation job
/// </summary>
public class WiredBroadbandReportJobRequestDto
{
    /// <summary>
    /// Unique identifier for the job (Hangfire Job ID)
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who initiated the job
    /// </summary>
    public string UserId { get; set; } = "System";

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
    /// Filter by connectivity status. When null, includes all statuses.
    /// </summary>
    public ServiceStatusEnum? FilterByStatus { get; set; }

    /// <summary>
    /// Report format (Excel or PDF)
    /// </summary>
    public ReportFormat Format { get; set; } = ReportFormat.Excel;

    /// <summary>
    /// Custom report title (optional)
    /// </summary>
    public string? ReportTitle { get; set; }

    /// <summary>
    /// When true, includes the Remarks column in the report
    /// </summary>
    public bool IncludeRemarks { get; set; } = false;

    /// <summary>
    /// When true, forces generation of new connectivity assessments
    /// </summary>
    public bool ForceGenerateConnectivityAssessment { get; set; } = false;
}
