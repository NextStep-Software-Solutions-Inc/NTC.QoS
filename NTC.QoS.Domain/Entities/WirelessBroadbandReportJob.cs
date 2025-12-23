using System.ComponentModel.DataAnnotations;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Entity for tracking wireless broadband report generation jobs
/// </summary>
public class WirelessBroadbandReportJob : BaseEntity
{
    /// <summary>
    /// Hangfire job ID for tracking
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string HangfireJobId { get; set; } = string.Empty;

    /// <summary>
    /// User who requested the report
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the job
    /// </summary>
    [Required]
    public ReportJobStatusEnum Status { get; set; } = ReportJobStatusEnum.Queued;

    /// <summary>
    /// Region ID for the report (if region-level)
    /// </summary>
    public Guid? RegionId { get; set; }

    /// <summary>
    /// Province ID for the report (if province-level)
    /// </summary>
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// Municipality ID for the report (if municipality-level)
    /// </summary>
    public Guid? MunicipalityId { get; set; }

    /// <summary>
    /// Report format (PDF or Excel)
    /// </summary>
    [Required]
    public ReportFormatEnum Format { get; set; }

    /// <summary>
    /// Optional filter by service status
    /// </summary>
    public ServiceStatusEnum? FilterByStatus { get; set; }

    /// <summary>
    /// Generated report file path (after completion)
    /// </summary>
    [MaxLength(500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// Generated report file name
    /// </summary>
    [MaxLength(200)]
    public string? FileName { get; set; }

    /// <summary>
    /// When the job started processing
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if job failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of barangays processed
    /// </summary>
    public int BarangaysProcessed { get; set; }

    /// <summary>
    /// Total time taken in milliseconds
    /// </summary>
    public long? ProcessingTimeMs { get; set; }
}
