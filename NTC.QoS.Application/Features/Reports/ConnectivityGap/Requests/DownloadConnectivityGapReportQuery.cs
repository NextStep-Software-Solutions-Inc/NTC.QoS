using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Reports.ConnectivityGap.Requests;

/// <summary>
/// Query to download connectivity gap report as PDF or Excel file
/// </summary>
public class DownloadConnectivityGapReportQuery : IRequest<ApiResponse<ConnectivityGapReportDownloadResult>>
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
    /// Report format (Excel or Pdf)
    /// </summary>
    public ReportFormat Format { get; set; } = ReportFormat.Excel;

    /// <summary>
    /// Optional custom title for the report
    /// </summary>
    public string? ReportTitle { get; set; }
}

/// <summary>
/// Result of connectivity gap report download containing file data and metadata
/// </summary>
public class ConnectivityGapReportDownloadResult
{
    /// <summary>
    /// File content as byte array
    /// </summary>
    public byte[] FileContent { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Generated file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type for the file
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}
