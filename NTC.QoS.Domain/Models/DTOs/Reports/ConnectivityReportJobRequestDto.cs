using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Request DTO for connectivity report generation job
/// </summary>
public class ConnectivityReportJobRequestDto
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
    /// Report format (Excel or PDF)
    /// </summary>
    public ReportFormat Format { get; set; } = ReportFormat.Excel;

    /// <summary>
    /// Custom report title (optional)
    /// </summary>
    public string? ReportTitle { get; set; }
}
