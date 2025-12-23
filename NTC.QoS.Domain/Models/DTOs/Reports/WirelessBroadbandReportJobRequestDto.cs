using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Request DTO for wireless broadband report background job
/// </summary>
public class WirelessBroadbandReportJobRequestDto
{
    public string JobId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid? RegionId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? MunicipalityId { get; set; }
    public ServiceStatusEnum? FilterByStatus { get; set; }
    public ReportFormatEnum Format { get; set; }
    public bool IncludeScores { get; set; }
}
