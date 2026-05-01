using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Reports.Connectivity.Requests;

/// <summary>
/// Query to download the unified connectivity report as PDF or Excel file
/// </summary>
public class DownloadConnectivityReportQuery : IRequest<ApiResponse<ConnectivityReportDownloadResult>>
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
    /// Optional filter by connectivity status. When null, all statuses are included.
    /// Served = 3, Underserved = 2, Unserved = 1
    /// </summary>
    public ConnectivityStatusEnum? FilterByStatus { get; set; }

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
/// Result of connectivity report download containing file data and metadata
/// </summary>
public class ConnectivityReportDownloadResult
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
