using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for generating various report formats (PDF, Excel) from report data
/// </summary>
public interface IReportGenerationService
{
    /// <summary>
    /// Generate a PDF report from wired broadband data
    /// </summary>
    /// <param name="reportData">The wired broadband report data</param>
    /// <param name="reportTitle">Title for the report</param>
    /// <param name="includeRemarks">When true, includes the remarks column in the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateWiredBroadbandPdfAsync(
        WiredBroadbandReportDto reportData, 
        string reportTitle = "Wired Broadband Report",
        bool includeRemarks = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate an Excel report from wired broadband data
    /// </summary>
    /// <param name="reportData">The wired broadband report data</param>
    /// <param name="reportTitle">Title for the report</param>
    /// <param name="includeRemarks">When true, includes the remarks column in the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> GenerateWiredBroadbandExcelAsync(
        WiredBroadbandReportDto reportData,
        string reportTitle = "Wired Broadband Report",
        bool includeRemarks = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a connectivity gap PDF report
    /// </summary>
    /// <param name="reportData">The connectivity gap report data</param>
    /// <param name="reportTitle">Title for the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateConnectivityGapPdfAsync(
        ConnectivityGapReportDto reportData,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a connectivity gap Excel report
    /// </summary>
    /// <param name="reportData">The connectivity gap report data</param>
    /// <param name="reportTitle">Title for the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> GenerateConnectivityGapExcelAsync(
        ConnectivityGapReportDto reportData,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a connectivity PDF report with rubric score breakdown
    /// </summary>
    /// <param name="reportData">The connectivity report data</param>
    /// <param name="reportTitle">Title for the report</param>
    /// <param name="filterByStatus">Optional status filter; when set, the Areas table heading reflects the active filter (e.g., "Served Areas").</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateConnectivityPdfAsync(
        ConnectivityReportDto reportData,
        string reportTitle = "Connectivity Report",
        ConnectivityStatusEnum? filterByStatus = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a connectivity Excel report with rubric score breakdown
    /// </summary>
    /// <param name="reportData">The connectivity report data</param>
    /// <param name="reportTitle">Title for the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> GenerateConnectivityExcelAsync(
        ConnectivityReportDto reportData,
        string reportTitle = "Connectivity Report",
        CancellationToken cancellationToken = default);
}