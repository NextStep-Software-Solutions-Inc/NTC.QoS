using ClosedXML.Excel;
using IronPdf;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Reports;
using System.Text;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for generating PDF and Excel reports from various report data types
/// </summary>
public class ReportGenerationService : IReportGenerationService
{
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(ILogger<ReportGenerationService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> GenerateWiredBroadbandPdfAsync(
        WiredBroadbandReportDto reportData, 
        string reportTitle = "Wired Broadband Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF report: {ReportTitle}", reportTitle);

        var html = GenerateWiredBroadbandHtml(reportData, reportTitle);
        
        var renderer = new ChromePdfRenderer();
        var pdfDocument = await Task.Run(() => renderer.RenderHtmlAsPdf(html), cancellationToken);
        var pdfBytes = pdfDocument.BinaryData;

        _logger.LogInformation("PDF report generated successfully. Size: {FileSize} bytes", pdfBytes.Length);
        return pdfBytes;
    }

    public async Task<byte[]> GenerateWiredBroadbandExcelAsync(
        WiredBroadbandReportDto reportData,
        string reportTitle = "Wired Broadband Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Excel report: {ReportTitle}", reportTitle);

        using var workbook = new XLWorkbook();
        
        // Create Summary worksheet
        CreateSummaryWorksheet(workbook, reportData.Summary, reportTitle);
        
        // Create detailed data worksheet
        CreateDetailedDataWorksheet(workbook, reportData);

        // Create provider coverage worksheet  
        CreateProviderCoverageWorksheet(workbook, reportData);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var excelBytes = stream.ToArray();

        _logger.LogInformation("Excel report generated successfully. Size: {FileSize} bytes", excelBytes.Length);
        
        await Task.CompletedTask;
        return excelBytes;
    }

    public async Task<byte[]> GenerateConnectivityGapPdfAsync(
        ConnectivityGapReportDto reportData,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating connectivity gap PDF report: {ReportTitle}", reportTitle);

        var html = GenerateConnectivityGapHtml(reportData, reportTitle);
        
        var renderer = new ChromePdfRenderer();
        var pdfDocument = await Task.Run(() => renderer.RenderHtmlAsPdf(html), cancellationToken);
        var pdfBytes = pdfDocument.BinaryData;

        _logger.LogInformation("Connectivity gap PDF report generated successfully. Size: {FileSize} bytes", pdfBytes.Length);
        return pdfBytes;
    }

    public async Task<byte[]> GenerateConnectivityGapExcelAsync(
        ConnectivityGapReportDto reportData,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating connectivity gap Excel report: {ReportTitle}", reportTitle);

        using var workbook = new XLWorkbook();
        
        // Create gap analysis summary
        CreateGapAnalysisSummaryWorksheet(workbook, reportData, reportTitle);
        
        // Create priority areas worksheet
        CreatePriorityAreasWorksheet(workbook, reportData);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var excelBytes = stream.ToArray();

        _logger.LogInformation("Connectivity gap Excel report generated successfully. Size: {FileSize} bytes", excelBytes.Length);
        
        await Task.CompletedTask;
        return excelBytes;
    }

    #region Private HTML Generation Methods

    private string GenerateWiredBroadbandHtml(WiredBroadbandReportDto reportData, string reportTitle)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine($"<title>{reportTitle}</title>");
        html.AppendLine(GetReportStyles());
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine("<div class='logo-section'>");
        html.AppendLine("<h1>NATIONAL TELECOMMUNICATIONS COMMISSION</h1>");
        html.AppendLine("<h2>Quality of Service Management System</h2>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");
        
        // Report Title and Summary
        html.AppendLine($"<h1 class='report-title'>{reportTitle}</h1>");
        html.AppendLine($"<p class='generation-date'>Generated: {reportData.Summary.GeneratedAt:MMMM dd, yyyy 'at' h:mm tt}</p>");
        
        // Executive Summary
        html.AppendLine("<div class='summary-section'>");
        html.AppendLine("<h2>Executive Summary</h2>");
        html.AppendLine("<div class='summary-grid'>");
        html.AppendLine($"<div class='summary-card'><h3>{reportData.Summary.TotalMunicipalities}</h3><p>Municipalities</p></div>");
        html.AppendLine($"<div class='summary-card'><h3>{reportData.Summary.TotalBarangays}</h3><p>Total Barangays</p></div>");
        html.AppendLine($"<div class='summary-card served'><h3>{reportData.Summary.ServedBarangays} ({reportData.Summary.ServedPercentage:F1}%)</h3><p>Served</p></div>");
        html.AppendLine($"<div class='summary-card underserved'><h3>{reportData.Summary.UnderservedBarangays} ({reportData.Summary.UnderservedPercentage:F1}%)</h3><p>Underserved</p></div>");
        html.AppendLine($"<div class='summary-card unserved'><h3>{reportData.Summary.UnservedBarangays} ({reportData.Summary.UnservedPercentage:F1}%)</h3><p>Unserved</p></div>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Provider Coverage Summary
        if (reportData.Summary.AllProviders.Count > 0)
        {
            html.AppendLine("<div class='provider-section'>");
            html.AppendLine("<h2>Provider Coverage Summary</h2>");
            html.AppendLine("<table class='provider-table'>");
            html.AppendLine("<thead><tr><th>Service Provider</th><th>Barangays Covered</th><th>Coverage %</th></tr></thead>");
            html.AppendLine("<tbody>");
            
            foreach (var provider in reportData.Summary.AllProviders.Take(10)) // Top 10 providers
            {
                var coverage = reportData.Summary.ProviderCoverage.GetValueOrDefault(provider, 0);
                var percentage = reportData.Summary.TotalBarangays > 0 ? (double)coverage / reportData.Summary.TotalBarangays * 100 : 0;
                html.AppendLine($"<tr><td>{provider}</td><td>{coverage}</td><td>{percentage:F1}%</td></tr>");
            }
            
            html.AppendLine("</tbody></table>");
            html.AppendLine("</div>");
        }

        // Detailed Municipality Data (First page sample)
        html.AppendLine("<div class='details-section'>");
        html.AppendLine("<h2>Municipality Breakdown (Sample - Top 10)</h2>");
        html.AppendLine("<table class='municipality-table'>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr>");
        html.AppendLine("<th rowspan='2'>Municipality</th>");
        html.AppendLine("<th colspan='6'>WIRED BROADBAND</th>");
        html.AppendLine("<th rowspan='2'>Remarks</th>");
        html.AppendLine("</tr>");
        html.AppendLine("<tr>");
        html.AppendLine("<th style='background-color: #FF6B6B;'>PLDT</th>");
        html.AppendLine("<th style='background-color: #4ECDC4;'>GLOBE</th>");
        html.AppendLine("<th style='background-color: #45B7D1;'>CONVERGE</th>");
        html.AppendLine("<th style='background-color: #96CEB4;'>PARASAT</th>");
        html.AppendLine("<th style='background-color: #FFEAA7;'>DCTECH</th>");
        html.AppendLine("<th style='background-color: #DDA0DD;'>OTHERS</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead>");
        html.AppendLine("<tbody>");

        foreach (var municipality in reportData.Municipalities.Take(10))
        {
            foreach (var barangay in municipality.Barangays.Take(5)) // Show first 5 barangays per municipality
            {
                var providers = barangay.ProviderAvailability.Providers;
                var statusClass = GetStatusClass(barangay.ServiceStatus);
                
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{municipality.Name} - {barangay.Name}</td>");
                html.AppendLine($"<td class='provider-cell'>{(providers.GetValueOrDefault("PLDT", false) ? "?" : "")}</td>");
                html.AppendLine($"<td class='provider-cell'>{(providers.GetValueOrDefault("GLOBE", false) ? "?" : "")}</td>");
                html.AppendLine($"<td class='provider-cell'>{(providers.GetValueOrDefault("CONVERGE", false) ? "?" : "")}</td>");
                html.AppendLine($"<td class='provider-cell'>{(providers.GetValueOrDefault("PARASAT", false) ? "?" : "")}</td>");
                html.AppendLine($"<td class='provider-cell'>{(providers.GetValueOrDefault("DCTECH", false) ? "?" : "")}</td>");
                html.AppendLine($"<td class='provider-cell'>{(barangay.ProviderAvailability.AvailableProviders.Any(p => !new[] { "PLDT", "GLOBE", "CONVERGE", "PARASAT", "DCTECH" }.Contains(p)) ? "?" : "")}</td>");
                html.AppendLine($"<td class='{statusClass}'>{barangay.ServiceStatus.ToString().ToUpper()}</td>");
                html.AppendLine("</tr>");
            }
        }

        html.AppendLine("</tbody></table>");
        html.AppendLine("<p><em>Note: This table shows a sample of the data. Complete report available in Excel format.</em></p>");
        html.AppendLine("</div>");

        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private string GenerateConnectivityGapHtml(ConnectivityGapReportDto reportData, string reportTitle)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine($"<title>{reportTitle}</title>");
        html.AppendLine(GetReportStyles());
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine("<div class='logo-section'>");
        html.AppendLine("<h1>NATIONAL TELECOMMUNICATIONS COMMISSION</h1>");
        html.AppendLine("<h2>Quality of Service Management System</h2>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");
        
        html.AppendLine($"<h1 class='report-title'>{reportTitle}</h1>");
        html.AppendLine($"<p class='generation-date'>Generated: {reportData.GeneratedAt:MMMM dd, yyyy 'at' h:mm tt}</p>");
        
        // Gap Analysis Summary
        html.AppendLine("<div class='gap-summary-section'>");
        html.AppendLine("<h2>Connectivity Gap Overview</h2>");
        html.AppendLine("<div class='gap-grid'>");
        html.AppendLine($"<div class='gap-card unserved'><h3>{reportData.TotalUnservedAreas}</h3><p>Unserved Areas</p></div>");
        html.AppendLine($"<div class='gap-card underserved'><h3>{reportData.TotalUnderservedAreas}</h3><p>Underserved Areas</p></div>");
        html.AppendLine($"<div class='gap-card population'><h3>{reportData.UnservedPopulation:N0}</h3><p>Unserved Population</p></div>");
        html.AppendLine($"<div class='gap-card population'><h3>{reportData.UnderservedPopulation:N0}</h3><p>Underserved Population</p></div>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Priority Areas
        if (reportData.PriorityAreas.Count > 0)
        {
            html.AppendLine("<div class='priority-section'>");
            html.AppendLine("<h2>Priority Areas for Intervention</h2>");
            html.AppendLine("<table class='priority-table'>");
            html.AppendLine("<thead><tr><th>Area</th><th>Status</th><th>Population</th><th>Score</th><th>Last Assessed</th></tr></thead>");
            html.AppendLine("<tbody>");
            
            foreach (var area in reportData.PriorityAreas)
            {
                var statusClass = GetConnectivityStatusClass(area.ConnectivityStatus);
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{area.LocationName}</td>");
                html.AppendLine($"<td class='{statusClass}'>{area.ConnectivityStatus}</td>");
                html.AppendLine($"<td>{area.Population:N0}</td>");
                html.AppendLine($"<td>{area.TotalScore}</td>");
                html.AppendLine($"<td>{area.LastAssessed:MMM dd, yyyy}</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</tbody></table>");
            html.AppendLine("</div>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private string GetReportStyles()
    {
        return @"
        <style>
            body { 
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
                margin: 0; 
                padding: 20px; 
                font-size: 12px; 
                line-height: 1.4; 
            }
            .header { 
                text-align: center; 
                border-bottom: 3px solid #2c3e50; 
                padding-bottom: 20px; 
                margin-bottom: 30px; 
            }
            .header h1 { 
                color: #2c3e50; 
                font-size: 24px; 
                margin: 0; 
            }
            .header h2 { 
                color: #34495e; 
                font-size: 16px; 
                margin: 5px 0; 
            }
            .report-title { 
                color: #2c3e50; 
                text-align: center; 
                font-size: 20px; 
                margin: 20px 0; 
            }
            .generation-date { 
                text-align: center; 
                color: #7f8c8d; 
                margin-bottom: 30px; 
            }
            .summary-grid, .gap-grid { 
                display: flex; 
                justify-content: space-around; 
                margin: 20px 0; 
                flex-wrap: wrap; 
            }
            .summary-card, .gap-card { 
                border: 1px solid #ddd; 
                border-radius: 8px; 
                padding: 15px; 
                text-align: center; 
                min-width: 120px; 
                margin: 5px; 
            }
            .summary-card h3, .gap-card h3 { 
                font-size: 18px; 
                margin: 0; 
                color: #2c3e50; 
            }
            .summary-card.served { 
                background-color: #d5f4e6; 
                border-color: #27ae60; 
            }
            .summary-card.underserved { 
                background-color: #fdf2e1; 
                border-color: #f39c12; 
            }
            .summary-card.unserved { 
                background-color: #fadbd8; 
                border-color: #e74c3c; 
            }
            .gap-card.population { 
                background-color: #eaf2ff; 
                border-color: #3498db; 
            }
            table { 
                width: 100%; 
                border-collapse: collapse; 
                margin: 20px 0; 
                font-size: 11px; 
            }
            th, td { 
                border: 1px solid #ddd; 
                padding: 8px; 
                text-align: center; 
            }
            th { 
                background-color: #f8f9fa; 
                font-weight: bold; 
                color: #2c3e50; 
            }
            .provider-cell { 
                font-size: 14px; 
                font-weight: bold; 
                color: #27ae60; 
            }
            .served { 
                background-color: #d5f4e6; 
                color: #27ae60; 
                font-weight: bold; 
            }
            .underserved { 
                background-color: #fdf2e1; 
                color: #f39c12; 
                font-weight: bold; 
            }
            .unserved { 
                background-color: #fadbd8; 
                color: #e74c3c; 
                font-weight: bold; 
            }
            .municipality-table th { 
                font-size: 10px; 
            }
            .municipality-table td { 
                font-size: 10px; 
            }
            .section { 
                margin: 30px 0; 
            }
            @media print {
                body { 
                    font-size: 10px; 
                }
                .summary-grid, .gap-grid { 
                    display: block; 
                }
                .summary-card, .gap-card { 
                    display: inline-block; 
                    width: 18%; 
                }
            }
        </style>";
    }

    #endregion

    #region Private Excel Generation Methods

    private void CreateSummaryWorksheet(XLWorkbook workbook, WiredBroadbandSummaryDto summary, string reportTitle)
    {
        var worksheet = workbook.Worksheets.Add("Summary");
        
        // Report Title and Date
        worksheet.Cell(1, 1).Value = reportTitle;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 4).Merge();
        
        worksheet.Cell(2, 1).Value = $"Generated: {summary.GeneratedAt:MMMM dd, yyyy 'at' h:mm tt}";
        worksheet.Range(2, 1, 2, 4).Merge();
        
        // Summary Statistics
        worksheet.Cell(4, 1).Value = "EXECUTIVE SUMMARY";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Font.FontSize = 14;
        
        // Summary data rows
        worksheet.Cell(6, 1).Value = "Metric"; worksheet.Cell(6, 2).Value = "Count"; worksheet.Cell(6, 3).Value = "Percentage";
        worksheet.Cell(7, 1).Value = "Total Municipalities"; worksheet.Cell(7, 2).Value = summary.TotalMunicipalities; worksheet.Cell(7, 3).Value = "";
        worksheet.Cell(8, 1).Value = "Total Barangays"; worksheet.Cell(8, 2).Value = summary.TotalBarangays; worksheet.Cell(8, 3).Value = "100.0%";
        worksheet.Cell(9, 1).Value = "Served Barangays"; worksheet.Cell(9, 2).Value = summary.ServedBarangays; worksheet.Cell(9, 3).Value = $"{summary.ServedPercentage:F1}%";
        worksheet.Cell(10, 1).Value = "Underserved Barangays"; worksheet.Cell(10, 2).Value = summary.UnderservedBarangays; worksheet.Cell(10, 3).Value = $"{summary.UnderservedPercentage:F1}%";
        worksheet.Cell(11, 1).Value = "Unserved Barangays"; worksheet.Cell(11, 2).Value = summary.UnservedBarangays; worksheet.Cell(11, 3).Value = $"{summary.UnservedPercentage:F1}%";
        
        // Format headers
        worksheet.Range(6, 1, 6, 3).Style.Font.Bold = true;
        worksheet.Range(6, 1, 6, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Format status rows
        worksheet.Row(9).Style.Fill.BackgroundColor = XLColor.LightGreen;  // Served
        worksheet.Row(10).Style.Fill.BackgroundColor = XLColor.LightYellow; // Underserved
        worksheet.Row(11).Style.Fill.BackgroundColor = XLColor.LightPink;   // Unserved
        
        // Auto-fit columns
        worksheet.ColumnsUsed().AdjustToContents();
    }

    private void CreateDetailedDataWorksheet(XLWorkbook workbook, WiredBroadbandReportDto reportData)
    {
        var worksheet = workbook.Worksheets.Add("Detailed Data");
        
        // Headers
        var headers = new[] { "Municipality", "Barangay" };
        var providerHeaders = reportData.Summary.AllProviders.Take(10).ToArray(); // Limit to top 10 providers
        var finalHeaders = headers.Concat(providerHeaders).Concat(new[] { "Service Status", "Remarks" }).ToArray();
        
        for (int i = 0; i < finalHeaders.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = finalHeaders[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        
        // Data rows
        int row = 2;
        foreach (var municipality in reportData.Municipalities)
        {
            foreach (var barangay in municipality.Barangays)
            {
                worksheet.Cell(row, 1).Value = municipality.Name;
                worksheet.Cell(row, 2).Value = barangay.Name;
                
                // Provider availability
                for (int i = 0; i < providerHeaders.Length; i++)
                {
                    var hasProvider = barangay.ProviderAvailability.Providers.GetValueOrDefault(providerHeaders[i], false);
                    worksheet.Cell(row, i + 3).Value = hasProvider ? "?" : "";
                    if (hasProvider)
                    {
                        worksheet.Cell(row, i + 3).Style.Font.FontColor = XLColor.Green;
                        worksheet.Cell(row, i + 3).Style.Font.Bold = true;
                    }
                }
                
                worksheet.Cell(row, providerHeaders.Length + 3).Value = barangay.ServiceStatus.ToString();
                worksheet.Cell(row, providerHeaders.Length + 4).Value = barangay.Remarks;
                
                // Color code status
                var statusCell = worksheet.Cell(row, providerHeaders.Length + 3);
                switch (barangay.ServiceStatus)
                {
                    case ServiceStatusEnum.Served:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;
                    case ServiceStatusEnum.Underserved:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                        break;
                    case ServiceStatusEnum.Unserved:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                        break;
                }
                
                row++;
            }
        }
        
        // Auto-fit columns
        worksheet.ColumnsUsed().AdjustToContents();
    }

    private void CreateProviderCoverageWorksheet(XLWorkbook workbook, WiredBroadbandReportDto reportData)
    {
        var worksheet = workbook.Worksheets.Add("Provider Coverage");
        
        // Headers
        worksheet.Cell(1, 1).Value = "Service Provider";
        worksheet.Cell(1, 2).Value = "Barangays Covered";
        worksheet.Cell(1, 3).Value = "Coverage Percentage";
        worksheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Data
        int row = 2;
        foreach (var provider in reportData.Summary.AllProviders)
        {
            var coverage = reportData.Summary.ProviderCoverage.GetValueOrDefault(provider, 0);
            var percentage = reportData.Summary.TotalBarangays > 0 ? (double)coverage / reportData.Summary.TotalBarangays * 100 : 0;
            
            worksheet.Cell(row, 1).Value = provider;
            worksheet.Cell(row, 2).Value = coverage;
            worksheet.Cell(row, 3).Value = $"{percentage:F1}%";
            
            row++;
        }
        
        // Auto-fit columns
        worksheet.ColumnsUsed().AdjustToContents();
    }

    private void CreateGapAnalysisSummaryWorksheet(XLWorkbook workbook, ConnectivityGapReportDto reportData, string reportTitle)
    {
        var worksheet = workbook.Worksheets.Add("Gap Analysis");
        
        // Title
        worksheet.Cell(1, 1).Value = reportTitle;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 3).Merge();
        
        worksheet.Cell(2, 1).Value = $"Generated: {reportData.GeneratedAt:MMMM dd, yyyy 'at' h:mm tt}";
        worksheet.Range(2, 1, 2, 3).Merge();
        
        // Gap Summary data rows
        worksheet.Cell(4, 1).Value = "Gap Category"; worksheet.Cell(4, 2).Value = "Count"; worksheet.Cell(4, 3).Value = "Population Affected";
        worksheet.Cell(5, 1).Value = "Unserved Areas"; worksheet.Cell(5, 2).Value = reportData.TotalUnservedAreas; worksheet.Cell(5, 3).Value = reportData.UnservedPopulation;
        worksheet.Cell(6, 1).Value = "Underserved Areas"; worksheet.Cell(6, 2).Value = reportData.TotalUnderservedAreas; worksheet.Cell(6, 3).Value = reportData.UnderservedPopulation;
        worksheet.Cell(7, 1).Value = "Total Gap Areas"; worksheet.Cell(7, 2).Value = reportData.TotalUnservedAreas + reportData.TotalUnderservedAreas; worksheet.Cell(7, 3).Value = reportData.UnservedPopulation + reportData.UnderservedPopulation;
        
        // Format headers
        worksheet.Range(4, 1, 4, 3).Style.Font.Bold = true;
        worksheet.Range(4, 1, 4, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Format data rows
        worksheet.Row(5).Style.Fill.BackgroundColor = XLColor.LightPink;   // Unserved
        worksheet.Row(6).Style.Fill.BackgroundColor = XLColor.LightYellow; // Underserved
        worksheet.Row(7).Style.Fill.BackgroundColor = XLColor.LightBlue;   // Total
        worksheet.Row(7).Style.Font.Bold = true;
        
        // Auto-fit columns
        worksheet.ColumnsUsed().AdjustToContents();
    }

    private void CreatePriorityAreasWorksheet(XLWorkbook workbook, ConnectivityGapReportDto reportData)
    {
        var worksheet = workbook.Worksheets.Add("Priority Areas");
        
        // Headers
        var headers = new[] { "Area Name", "Connectivity Status", "Population", "Total Score", "Last Assessed" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        
        // Data
        int row = 2;
        foreach (var area in reportData.PriorityAreas)
        {
            worksheet.Cell(row, 1).Value = area.LocationName;
            worksheet.Cell(row, 2).Value = area.ConnectivityStatus.ToString();
            worksheet.Cell(row, 3).Value = area.Population ?? 0;
            worksheet.Cell(row, 4).Value = area.TotalScore;
            worksheet.Cell(row, 5).Value = area.LastAssessed.ToString("MMM dd, yyyy");
            
            // Color code by status
            var statusCell = worksheet.Cell(row, 2);
            switch (area.ConnectivityStatus)
            {
                case NTC.QoS.Domain.Enums.ConnectivityStatusEnum.Unserved:
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                    break;
                case NTC.QoS.Domain.Enums.ConnectivityStatusEnum.Underserved:
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    break;
            }
            
            row++;
        }
        
        // Auto-fit columns
        worksheet.ColumnsUsed().AdjustToContents();
    }

    #endregion

    #region Helper Methods

    private string GetStatusClass(ServiceStatusEnum status)
    {
        return status switch
        {
            ServiceStatusEnum.Served => "served",
            ServiceStatusEnum.Underserved => "underserved",
            ServiceStatusEnum.Unserved => "unserved",
            _ => "unserved"
        };
    }

    private string GetConnectivityStatusClass(NTC.QoS.Domain.Enums.ConnectivityStatusEnum status)
    {
        return status switch
        {
            NTC.QoS.Domain.Enums.ConnectivityStatusEnum.Served => "served",
            NTC.QoS.Domain.Enums.ConnectivityStatusEnum.Underserved => "underserved",
            NTC.QoS.Domain.Enums.ConnectivityStatusEnum.Unserved => "unserved",
            _ => "unserved"
        };
    }

    #endregion
}