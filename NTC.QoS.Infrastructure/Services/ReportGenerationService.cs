using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for generating PDF and Excel reports from various report data types
/// </summary>
public class ReportGenerationService : IReportGenerationService
{
    private readonly ILogger<ReportGenerationService> _logger;
    private readonly IProviderRepository _providerRepository;

    public ReportGenerationService(ILogger<ReportGenerationService> logger, IProviderRepository providerRepository)
    {
        _logger = logger;
        _providerRepository = providerRepository;
    }

    public async Task<byte[]> GenerateWiredBroadbandPdfAsync(
        WiredBroadbandReportDto reportData,
        string reportTitle = "Wired Broadband Report",
        bool includeRemarks = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF report: {ReportTitle}", reportTitle);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeWiredBroadbandHeader(c, reportTitle, reportData.Summary.GeneratedAt));
                page.Content().Element(c => ComposeWiredBroadbandContent(c, reportData));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        var pdfBytes = await Task.Run(() => document.GeneratePdf(), cancellationToken);

        _logger.LogInformation("PDF report generated successfully. Size: {FileSize} bytes", pdfBytes.Length);
        return pdfBytes;
    }

    public async Task<byte[]> GenerateWiredBroadbandExcelAsync(
        WiredBroadbandReportDto reportData,
        string reportTitle = "Wired Broadband Report",
        bool includeRemarks = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Excel report: {ReportTitle}", reportTitle);

        using var workbook = new XLWorkbook();
        
        // Create Summary worksheet
        CreateSummaryWorksheet(workbook, reportData.Summary, reportTitle);
        
        // Create detailed data worksheet
        await CreateDetailedDataWorksheetAsync(workbook, reportData, includeRemarks);

        // Create provider coverage worksheet  
        CreateProviderCoverageWorksheet(workbook, reportData);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var excelBytes = stream.ToArray();

        _logger.LogInformation("Excel report generated successfully. Size: {FileSize} bytes", excelBytes.Length);
        
        return excelBytes;
    }

    public async Task<byte[]> GenerateConnectivityGapPdfAsync(
        ConnectivityGapReportDto reportData,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating connectivity gap PDF report: {ReportTitle}", reportTitle);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeConnectivityGapHeader(c, reportTitle, reportData.GeneratedAt));
                page.Content().Element(c => ComposeConnectivityGapContent(c, reportData));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        var pdfBytes = await Task.Run(() => document.GeneratePdf(), cancellationToken);

        _logger.LogInformation("Connectivity gap PDF report generated successfully. Size: {FileSize} bytes", pdfBytes.Length);
        return pdfBytes;
    }

    public async Task<byte[]> GenerateConnectivityGapExcelAsync(
        ConnectivityGapReportDto reportData,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
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
            
            return excelBytes;
        }, cancellationToken);
    }

    #region Private QuestPDF Composition Methods

    private void ComposeWiredBroadbandHeader(IContainer container, string reportTitle, DateTime generatedAt)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(2).BorderColor(Colors.Grey.Darken2).PaddingBottom(10).Column(headerColumn =>
            {
                headerColumn.Item().AlignCenter().Text("NATIONAL TELECOMMUNICATIONS COMMISSION")
                    .FontSize(16).Bold().FontColor(Colors.Grey.Darken3);
                headerColumn.Item().AlignCenter().Text("Quality of Service Management System")
                    .FontSize(12).FontColor(Colors.Grey.Darken1);
            });

            column.Item().PaddingTop(10).PaddingBottom(5).AlignCenter().Text(reportTitle)
                .FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
            column.Item().AlignCenter().Text($"Generated: {generatedAt:MMMM dd, yyyy 'at' h:mm tt}")
                .FontSize(9).FontColor(Colors.Grey.Medium);
        });
    }

    private void ComposeWiredBroadbandContent(IContainer container, WiredBroadbandReportDto reportData)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Executive Summary
            column.Item().Text("Executive Summary").FontSize(12).Bold();
            column.Item().PaddingTop(5).Row(row =>
            {
                row.AutoItem().PaddingRight(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text(reportData.Summary.TotalMunicipalities.ToString()).FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Municipalities").FontSize(8);
                });
                row.AutoItem().PaddingRight(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text(reportData.Summary.TotalBarangays.ToString()).FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Total Barangays").FontSize(8);
                });
                row.AutoItem().PaddingRight(10).Border(1).BorderColor(Colors.Green.Lighten3).Background(Colors.Green.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text($"{reportData.Summary.ServedBarangays} ({reportData.Summary.ServedPercentage:F1}%)").FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Served").FontSize(8);
                });
                row.AutoItem().PaddingRight(10).Border(1).BorderColor(Colors.Orange.Lighten2).Background(Colors.Orange.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text($"{reportData.Summary.UnderservedBarangays} ({reportData.Summary.UnderservedPercentage:F1}%)").FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Underserved").FontSize(8);
                });
                row.AutoItem().Border(1).BorderColor(Colors.Red.Lighten2).Background(Colors.Red.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text($"{reportData.Summary.UnservedBarangays} ({reportData.Summary.UnservedPercentage:F1}%)").FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Unserved").FontSize(8);
                });
            });

            // Provider Coverage Summary
            if (reportData.Summary.AllProviders.Count > 0)
            {
                column.Item().PaddingTop(15).Text("Provider Coverage Summary").FontSize(12).Bold();
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Service Provider").Bold();
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Barangays Covered").Bold();
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Coverage %").Bold();
                    });

                    foreach (var provider in reportData.Summary.AllProviders.Take(10))
                    {
                        var coverage = reportData.Summary.ProviderCoverage.GetValueOrDefault(provider, 0);
                        var percentage = reportData.Summary.TotalBarangays > 0 ? (double)coverage / reportData.Summary.TotalBarangays * 100 : 0;

                        table.Cell().Border(1).Padding(5).Text(provider);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(coverage.ToString());
                        table.Cell().Border(1).Padding(5).AlignCenter().Text($"{percentage:F1}%");
                    }
                });
            }

            // Municipality Breakdown
            column.Item().PaddingTop(15).Text("Municipality Breakdown").FontSize(12).Bold();

            // Municipality Sample Table
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Location").Bold();
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Providers").Bold();
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Service Status").Bold();
                });

                foreach (var municipality in reportData.Municipalities)
                {
                    foreach (var barangay in municipality.Barangays)
                    {
                        var providersText = string.Join(", ", barangay.ProviderAvailability.AvailableProviders);
                        var statusColor = barangay.ServiceStatus switch
                        {
                            ServiceStatusEnum.Served => Colors.Green.Lighten4,
                            ServiceStatusEnum.Underserved => Colors.Orange.Lighten4,
                            _ => Colors.Red.Lighten4
                        };

                        table.Cell().Border(1).Padding(5).Text($"{municipality.Name} - {barangay.Name}").FontSize(8);
                        table.Cell().Border(1).Padding(5).Text(providersText).FontSize(7);
                        table.Cell().Border(1).Background(statusColor).Padding(5).AlignCenter().Text(barangay.ServiceStatus.ToString().ToUpper()).FontSize(8).Bold();
                    }
                }
            });
        });
    }

    private void ComposeConnectivityGapHeader(IContainer container, string reportTitle, DateTime generatedAt)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(2).BorderColor(Colors.Grey.Darken2).PaddingBottom(10).Column(headerColumn =>
            {
                headerColumn.Item().AlignCenter().Text("NATIONAL TELECOMMUNICATIONS COMMISSION")
                    .FontSize(16).Bold().FontColor(Colors.Grey.Darken3);
                headerColumn.Item().AlignCenter().Text("Quality of Service Management System")
                    .FontSize(12).FontColor(Colors.Grey.Darken1);
            });

            column.Item().PaddingTop(10).PaddingBottom(5).AlignCenter().Text(reportTitle)
                .FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
            column.Item().AlignCenter().Text($"Generated: {generatedAt:MMMM dd, yyyy 'at' h:mm tt}")
                .FontSize(9).FontColor(Colors.Grey.Medium);
        });
    }

    private void ComposeConnectivityGapContent(IContainer container, ConnectivityGapReportDto reportData)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Gap Overview
            column.Item().Text("Connectivity Gap Overview").FontSize(12).Bold();
            column.Item().PaddingTop(5).Row(row =>
            {
                row.AutoItem().PaddingRight(10).Border(1).BorderColor(Colors.Red.Lighten2).Background(Colors.Red.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text(reportData.Summary.TotalUnservedAreas.ToString()).FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Unserved Areas").FontSize(8);
                });
                row.AutoItem().PaddingRight(10).Border(1).BorderColor(Colors.Orange.Lighten2).Background(Colors.Orange.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text(reportData.Summary.TotalUnderservedAreas.ToString()).FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Underserved Areas").FontSize(8);
                });
                row.AutoItem().PaddingRight(10).Border(1).BorderColor(Colors.Blue.Lighten2).Background(Colors.Blue.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text(reportData.Summary.UnservedPopulation.ToString("N0")).FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Unserved Population").FontSize(8);
                });
                row.AutoItem().Border(1).BorderColor(Colors.Blue.Lighten2).Background(Colors.Blue.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text(reportData.Summary.UnderservedPopulation.ToString("N0")).FontSize(14).Bold();
                    col.Item().AlignCenter().Text("Underserved Population").FontSize(8);
                });
            });

            // Priority Areas
            if (reportData.PriorityAreas.Count > 0)
            {
                column.Item().PaddingTop(15).Text("Priority Areas for Intervention").FontSize(12).Bold();
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Area").Bold();
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Status").Bold();
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Population").Bold();
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Score").Bold();
                        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Last Assessed").Bold();
                    });

                    foreach (var area in reportData.PriorityAreas)
                    {
                        var statusColor = area.ConnectivityStatus switch
                        {
                            NTC.QoS.Domain.Enums.ConnectivityStatusEnum.Underserved => Colors.Orange.Lighten4,
                            NTC.QoS.Domain.Enums.ConnectivityStatusEnum.Unserved => Colors.Red.Lighten4,
                            _ => Colors.Grey.Lighten4
                        };

                        table.Cell().Border(1).Padding(5).Text(area.LocationName).FontSize(8);
                        table.Cell().Border(1).Background(statusColor).Padding(5).AlignCenter().Text(area.ConnectivityStatus.ToString()).FontSize(8);
                        table.Cell().Border(1).Padding(5).AlignRight().Text((area.Population ?? 0).ToString("N0")).FontSize(8);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(area.TotalScore.ToString()).FontSize(8);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(area.LastAssessed.ToString("MMM dd, yyyy")).FontSize(8);
                    }
                });
            }
        });
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

    /// <summary>  
    /// Creates the detailed data worksheet in the Excel workbook.  
    /// </summary>  
    /// <param name="workbook">The Excel workbook to add the worksheet to.</param>  
    /// <param name="reportData">The report data to populate the worksheet.</param>  
    /// <param name=\"includeRemarks\">When true, includes the Remarks column in the worksheet.</param>  
    /// <param name=\"cancellationToken\">Cancellation token</param>  
    private async Task CreateDetailedDataWorksheetAsync(XLWorkbook workbook, WiredBroadbandReportDto reportData, bool includeRemarks = true, CancellationToken cancellationToken = default)
    {
        var worksheet = workbook.Worksheets.Add("Detailed Data");
 
        // CRITICAL FIX: Use ProviderCoverage.Keys instead of AllProviders
        // ProviderCoverage only contains providers with actual coverage (FiberNAPCount > 0)
        // AllProviders may include providers with zero coverage
        var providerHeaders = reportData.Summary.ProviderCoverage.Keys.OrderBy(p => p).ToArray();
     
        _logger.LogInformation("EXCEL GENERATION - Using ProviderCoverage.Keys for columns");
        _logger.LogInformation("EXCEL GENERATION - Provider count: {Count}", providerHeaders.Length);
        _logger.LogInformation("EXCEL GENERATION - Providers: {Providers}", string.Join(", ", providerHeaders));
        _logger.LogInformation("EXCEL GENERATION - AllProviders list (should be longer): {AllProviders}", 
            string.Join(", ", reportData.Summary.AllProviders));
        _logger.LogInformation("EXCEL GENERATION - ProviderCoverage dictionary:");
        foreach (var kvp in reportData.Summary.ProviderCoverage.OrderBy(p => p.Key))
        {
            _logger.LogInformation("  - {Provider}: {Coverage} barangays", kvp.Key, kvp.Value);
        }

        // CRITICAL: Handle case when no providers found (prevents Excel corruption)
        var hasProviders = providerHeaders.Length > 0;
        if (!hasProviders)
        {
            _logger.LogWarning("EXCEL GENERATION - No providers found! Adding placeholder column to prevent Excel corruption.");
            providerHeaders = new[] { "No Providers" }; // Add placeholder
        }

        // Fetch provider colors from database
        var providerColors = await FetchProviderColorsAsync(providerHeaders, cancellationToken);
 
        var wiredBroadbandStartCol = 3;
        var wiredBroadbandEndCol = 2 + providerHeaders.Length;
        var serviceStatusColumn = wiredBroadbandEndCol + 1;
        var remarksCol = includeRemarks ? serviceStatusColumn + 1 : 0;
        var lastCol = includeRemarks ? remarksCol : serviceStatusColumn;
     
        // ROW 1: Setup headers
        // Municipality header (spans rows 1-3) - NO rotation for header
        worksheet.Cell(1, 1).Value = "Municipality";
        worksheet.Range(1, 1, 3, 1).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 12;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
 
        // Barangay header (spans rows 1-3)
        worksheet.Cell(1, 2).Value = "Barangay";
        worksheet.Range(1, 2, 3, 2).Merge();
        worksheet.Cell(1, 2).Style.Font.Bold = true;
        worksheet.Cell(1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
   
        // "WIRED BROADBAND" header spanning all provider columns (row 1 only)
        worksheet.Range(1, wiredBroadbandStartCol, 1, wiredBroadbandEndCol).Merge();
        worksheet.Cell(1, wiredBroadbandStartCol).Value = "WIRED BROADBAND";
        worksheet.Cell(1, wiredBroadbandStartCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(1, wiredBroadbandStartCol).Style.Font.Bold = true;
        worksheet.Cell(1, wiredBroadbandStartCol).Style.Font.FontSize = 14;
        worksheet.Cell(1, wiredBroadbandStartCol).Style.Fill.BackgroundColor = XLColor.Orange;
    
        // CONNECTIVITY STATUS header (spans rows 1-3)
        worksheet.Cell(1, serviceStatusColumn).Value = "SERVICE STATUS";
        worksheet.Range(1, serviceStatusColumn, 3, serviceStatusColumn).Merge();
        worksheet.Cell(1, serviceStatusColumn).Style.Font.Bold = true;
        worksheet.Cell(1, serviceStatusColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(1, serviceStatusColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Cell(1, serviceStatusColumn).Style.Alignment.WrapText = true;

        // REMARKS header (spans rows 1-3) - conditionally included
        if (includeRemarks)
        {
            worksheet.Cell(1, remarksCol).Value = "REMARKS";
            worksheet.Range(1, remarksCol, 3, remarksCol).Merge();
            worksheet.Cell(1, remarksCol).Style.Font.Bold = true;
            worksheet.Cell(1, remarksCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(1, remarksCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        // ROW 2: Provider names with colors
        for (int i = 0; i < providerHeaders.Length; i++)
        {
            var cell = worksheet.Cell(2, wiredBroadbandStartCol + i);
            cell.Value = providerHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true; // Enable text wrapping for long names

            // Apply provider color if available
            if (providerColors.TryGetValue(providerHeaders[i], out var color))
            {
                cell.Style.Fill.BackgroundColor = color;
                cell.Style.Font.FontColor = XLColor.White;
            }
            else
            {
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
        }
   
        // ROW 3: "FTTH" labels under each provider
        for (int i = 0; i < providerHeaders.Length; i++)
        {
            var cell = worksheet.Cell(3, wiredBroadbandStartCol + i);
            cell.Value = "FTTH";
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        
        int currentRow = 4; // Data starts from row 4
     
        // Data rows with merged municipality cells
        foreach (var municipality in reportData.Municipalities)
        {
            var municipalityStartRow = currentRow;
            var barangayCount = municipality.Barangays.Count;
  
            foreach (var barangay in municipality.Barangays)
            {
                // Barangay name
                worksheet.Cell(currentRow, 2).Value = barangay.Name;
  
                // Provider checkmarks - CHECK ALL PROVIDERS (must match exact names from report)
                for (int i = 0; i < providerHeaders.Length; i++)
                {
                    var providerName = providerHeaders[i];
                    
                    // Skip checkmark logic if using placeholder (no real providers)
                    if (!hasProviders)
                    {
                        var cell = worksheet.Cell(currentRow, wiredBroadbandStartCol + i);
                        cell.Value = "N/A";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Font.FontColor = XLColor.Gray;
                        continue;
                    }
                    
                    var hasProvider = barangay.ProviderAvailability.Providers.GetValueOrDefault(providerName, false);
                    var cell2 = worksheet.Cell(currentRow, wiredBroadbandStartCol + i);
   
                    // Log first row for debugging
                    if (currentRow == 4)
                    {
                        _logger.LogDebug("Row {Row} - Barangay: {BarangayName}, Provider: {ProviderName}, HasProvider: {HasProvider}",
                            currentRow, barangay.Name, providerName, hasProvider);
                    }

                    if (hasProvider)
                    {
                        cell2.Value = "✓";  // Unicode checkmark symbol
                        cell2.Style.Font.FontSize = 14;
                        cell2.Style.Font.Bold = true;
                        cell2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell2.Style.Font.FontColor = XLColor.Green;
                    }
                }
  
                // Connectivity Status column (color-coded)
                var statusCell = worksheet.Cell(currentRow, serviceStatusColumn);
                statusCell.Value = barangay.ServiceStatus.ToString().ToUpper();
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                statusCell.Style.Font.Bold = true;
     
                switch (barangay.ServiceStatus)
                {
                    case ServiceStatusEnum.Served:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;
                    case ServiceStatusEnum.Underserved:
                        statusCell.Style.Fill.BackgroundColor = XLColor.Orange;
                        break;
                    case ServiceStatusEnum.Unserved:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                        break;
                }

                // Remarks column with actual remarks text - conditionally included
                if (includeRemarks)
                {
                    var remarksCell = worksheet.Cell(currentRow, remarksCol);
                    remarksCell.Value = barangay.Remarks ?? "";
                    remarksCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    remarksCell.Style.Alignment.WrapText = true;
                }
    
                currentRow++;
            }

            // Merge municipality cells for all barangays in this municipality
            if (barangayCount > 1)
            {
                worksheet.Range(municipalityStartRow, 1, currentRow - 1, 1).Merge();
            }
       
            worksheet.Cell(municipalityStartRow, 1).Value = municipality.Name;
            worksheet.Cell(municipalityStartRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell(municipalityStartRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(municipalityStartRow, 1).Style.Alignment.TextRotation = 90; // Rotate municipality name vertically
            worksheet.Cell(municipalityStartRow, 1).Style.Font.Bold = true;
            worksheet.Cell(municipalityStartRow, 1).Style.Font.FontSize = 16; // Larger font for municipality names

            // Set column width for municipality column to accommodate rotated text
            // Width is set once globally, not per-municipality

            // Add thick border around each municipality's data section
            var municipalityRange = worksheet.Range(municipalityStartRow, 1, currentRow - 1, lastCol);
            municipalityRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            municipalityRange.Style.Border.OutsideBorderColor = XLColor.Black;
        }
    
        // Apply borders to all cells
        var dataRange = worksheet.Range(1, 1, currentRow - 1, lastCol);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
  
        // Set column widths
        worksheet.Column(1).Width = 8; // Municipality - narrow width for rotated text
        worksheet.Column(2).Width = 30; // Barangay - increased for longer names

        for (int i = 0; i < providerHeaders.Length; i++)
        {
            worksheet.Column(wiredBroadbandStartCol + i).Width = 25; // Increased width for provider names
        }
      
        worksheet.Column(serviceStatusColumn).Width = 20; // Connectivity Status
        
        if (includeRemarks)
        {
            worksheet.Column(remarksCol).Width = 40; // Remarks - wider for text content
        }
  
    // Freeze first 3 rows (headers) and first 2 columns
    worksheet.SheetView.FreezeRows(3);
    worksheet.SheetView.FreezeColumns(2);
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
        worksheet.Cell(5, 1).Value = "Unserved Areas"; worksheet.Cell(5, 2).Value = reportData.Summary.TotalUnservedAreas; worksheet.Cell(5, 3).Value = reportData.Summary.UnservedPopulation;
        worksheet.Cell(6, 1).Value = "Underserved Areas"; worksheet.Cell(6, 2).Value = reportData.Summary.TotalUnderservedAreas; worksheet.Cell(6, 3).Value = reportData.Summary.UnderservedPopulation;
        worksheet.Cell(7, 1).Value = "Total Gap Areas"; worksheet.Cell(7, 2).Value = reportData.Summary.TotalUnservedAreas + reportData.Summary.TotalUnderservedAreas; worksheet.Cell(7, 3).Value = reportData.Summary.UnservedPopulation + reportData.Summary.UnderservedPopulation;
        
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

    /// <summary>
    /// Fetches provider colors from the database and converts them to XLColor
    /// Uses ONLY the providers that appear in the report's ProviderCoverage (providers with actual coverage)
    /// </summary>
    private async Task<Dictionary<string, XLColor>> FetchProviderColorsAsync(
          string[] providerNames,
          CancellationToken cancellationToken = default)
    {
        var providerColors = new Dictionary<string, XLColor>();

        try
        {
            // Fetch ONLY providers that are in the provider names list (from ProviderCoverage)
            // This ensures we don't fetch providers with zero coverage
            var providersQuery = _providerRepository.Query(p => providerNames.Contains(p.Name));
            var providers = await providersQuery.ToListAsync(cancellationToken);

            _logger.LogInformation("Fetched {Count} provider colors from database for providers: {Providers}", 
                providers.Count, string.Join(", ", providerNames));

            foreach (var provider in providers)
            {
                if (!string.IsNullOrWhiteSpace(provider.Color))
                {
                    try
                    {
                        var xlColor = ParseHexToXLColor(provider.Color);
                        providerColors[provider.Name] = xlColor;
                        _logger.LogDebug("Provider '{Provider}' color: {Color}", provider.Name, provider.Color);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to parse color '{Color}' for provider '{Provider}'. Using default gray.",
                            provider.Color, provider.Name);
                        providerColors[provider.Name] = XLColor.LightGray;
                    }
                }
                else
                {
                    _logger.LogDebug("Provider '{Provider}' has no color defined, using gray", provider.Name);
                    providerColors[provider.Name] = XLColor.LightGray;
                }
            }

            _logger.LogInformation("Successfully loaded colors for {Count} providers from database", providerColors.Count);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error fetching provider colors from database. Using default colors.");
        }

        return providerColors;
    }

    /// <summary>
    /// Parses a hex color string (e.g., "#FF6347" or "FF6347") to XLColor
    /// </summary>
    private static XLColor ParseHexToXLColor(string hexColor)
    {
        // Remove # if present
        hexColor = hexColor.TrimStart('#');

        // Handle different hex formats
        if (hexColor.Length == 6)
        {
            // RGB format: RRGGBB
            int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            return XLColor.FromArgb(r, g, b);
        }
        else if (hexColor.Length == 8)
        {
            // ARGB format: AARRGGBB
            int a = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int r = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(6, 2), 16);
            return XLColor.FromArgb(a, r, g, b);
        }
        else if (hexColor.Length == 3)
        {
            // Short RGB format: RGB -> RRGGBB
            int r = Convert.ToInt32(new string(hexColor[0], 2), 16);
            int g = Convert.ToInt32(new string(hexColor[1], 2), 16);
            int b = Convert.ToInt32(new string(hexColor[2], 2), 16);
            return XLColor.FromArgb(r, g, b);
        }
        else
        {
            throw new ArgumentException(
             $"Invalid hex color format: {hexColor}. Expected 3, 6, or 8 characters.");
        }
    }

    public async Task<byte[]> GenerateConnectivityPdfAsync(
        ConnectivityReportDto reportData,
        string reportTitle = "Connectivity Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating connectivity PDF report: {ReportTitle}", reportTitle);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Text(reportTitle).Bold().FontSize(16).AlignCenter();
                page.Content().Element(content => ComposeConnectivityContent(content, reportData));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        var pdfBytes = await Task.Run(() => document.GeneratePdf(), cancellationToken);

        _logger.LogInformation("Connectivity PDF report generated successfully. Size: {FileSize} bytes", pdfBytes.Length);
        return pdfBytes;
    }

    public async Task<byte[]> GenerateConnectivityExcelAsync(
        ConnectivityReportDto reportData,
        string reportTitle = "Connectivity Report",
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogInformation("Generating connectivity Excel report: {ReportTitle}", reportTitle);

            using var workbook = new XLWorkbook();
            
            // Create summary worksheet
            CreateConnectivitySummaryWorksheet(workbook, reportData, reportTitle);
            
            // Create detailed area data worksheet
            CreateConnectivityDetailWorksheet(workbook, reportData);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var excelBytes = stream.ToArray();

            _logger.LogInformation("Connectivity Excel report generated successfully. Size: {FileSize} bytes", excelBytes.Length);
            
            return excelBytes;
        }, cancellationToken);
    }

    private void ComposeConnectivityContent(IContainer container, ConnectivityReportDto reportData)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Summary Section
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Summary Statistics").Bold().FontSize(14);
                    col.Item().PaddingTop(5).Text($"Total Served Areas: {reportData.TotalServedAreas:N0}");
                    col.Item().Text($"Total Population Served: {reportData.ServedPopulation:N0}");
                    col.Item().Text($"Average Total Score: {reportData.ScoreSummary.AverageTotalScore:F2}/15");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Score Distribution").Bold().FontSize(14);
                    col.Item().PaddingTop(5).Text($"Excellent (13-15): {reportData.ScoreSummary.ExcellentCount:N0}");
                    col.Item().Text($"Good (11-12): {reportData.ScoreSummary.GoodCount:N0}");
                    col.Item().Text($"Moderate (8-10): {reportData.ScoreSummary.ModerateCount:N0}");
                    col.Item().Text($"Fair (6-7): {reportData.ScoreSummary.FairCount:N0}");
                });
            });

            // Average Rubric Scores
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Average Rubric Scores").Bold().FontSize(14);
                    col.Item().PaddingTop(5).Text($"Fiber Coverage: {reportData.ScoreSummary.AverageFiberCoverageScore:F2}/3");
                    col.Item().Text($"Internet Access: {reportData.ScoreSummary.AverageInternetAccessScore:F2}/3");
                    col.Item().Text($"Technology Type: {reportData.ScoreSummary.AverageTechnologyTypeScore:F2}/3");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("").FontSize(14);
                    col.Item().PaddingTop(5).Text($"Quality of Service: {reportData.ScoreSummary.AverageQualityOfServiceScore:F2}/3");
                    col.Item().Text($"Coverage Availability: {reportData.ScoreSummary.AverageCoverageAvailabilityScore:F2}/3");
                });
            });

            // All areas table
            column.Item()
                .Text(text => text.Span($"All Areas by Score ({reportData.Areas.Count} total)"))
                .Bold()
                .FontSize(14);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Location").Bold();
                    header.Cell().Element(CellStyle).Text("Pop").Bold();
                    header.Cell().Element(CellStyle).Text("Fiber").Bold();
                    header.Cell().Element(CellStyle).Text("Internet").Bold();
                    header.Cell().Element(CellStyle).Text("Tech").Bold();
                    header.Cell().Element(CellStyle).Text("QoS").Bold();
                    header.Cell().Element(CellStyle).Text("Cov").Bold();
                    header.Cell().Element(CellStyle).Text("Total").Bold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingVertical(5).Background(Colors.Grey.Lighten3);
                    }
                });

                // Data rows
                foreach (var area in reportData.Areas)
                {
                    table.Cell().Element(CellStyle).Text(area.LocationName).FontSize(8);
                    table.Cell().Element(CellStyle).Text(area.Population?.ToString("N0") ?? "N/A").FontSize(8);
                    table.Cell().Element(CellStyle).Text(area.FiberCoverageScore.ToString()).FontSize(8);
                    table.Cell().Element(CellStyle).Text(area.InternetAccessScore.ToString()).FontSize(8);
                    table.Cell().Element(CellStyle).Text(area.TechnologyTypeScore.ToString()).FontSize(8);
                    table.Cell().Element(CellStyle).Text(area.QualityOfServiceScore.ToString()).FontSize(8);
                    table.Cell().Element(CellStyle).Text(area.CoverageAvailabilityScore.ToString()).FontSize(8);
                    table.Cell().Element(CellStyle).Text(area.TotalScore.ToString()).Bold().FontSize(8);

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);
                    }
                }
            });
        });
    }

    private void CreateConnectivitySummaryWorksheet(XLWorkbook workbook, ConnectivityReportDto reportData, string reportTitle)
    {
        var worksheet = workbook.Worksheets.Add("Summary");

        // Title
        worksheet.Cell(1, 1).Value = reportTitle;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;

        // Summary Statistics
        int row = 3;
        worksheet.Cell(row, 1).Value = "Summary Statistics";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        
        row++;
        worksheet.Cell(row, 1).Value = "Total Served Areas:";
        worksheet.Cell(row, 2).Value = reportData.TotalServedAreas;
        
        row++;
        worksheet.Cell(row, 1).Value = "Total Population Served:";
        worksheet.Cell(row, 2).Value = reportData.ServedPopulation;
        
        row++;
        worksheet.Cell(row, 1).Value = "Average Total Score:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.AverageTotalScore;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";

        // Score Distribution
        row += 2;
        worksheet.Cell(row, 1).Value = "Score Distribution";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        
        row++;
        worksheet.Cell(row, 1).Value = "Excellent (13-15):";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.ExcellentCount;
        
        row++;
        worksheet.Cell(row, 1).Value = "Good (11-12):";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.GoodCount;
        
        row++;
        worksheet.Cell(row, 1).Value = "Moderate (8-10):";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.ModerateCount;
        
        row++;
        worksheet.Cell(row, 1).Value = "Fair (6-7):";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.FairCount;

        // Average Rubric Scores
        row += 2;
        worksheet.Cell(row, 1).Value = "Average Rubric Scores (0-3 each)";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        
        row++;
        worksheet.Cell(row, 1).Value = "Fiber Coverage:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.AverageFiberCoverageScore;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
        
        row++;
        worksheet.Cell(row, 1).Value = "Internet Access:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.AverageInternetAccessScore;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
        
        row++;
        worksheet.Cell(row, 1).Value = "Technology Type:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.AverageTechnologyTypeScore;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
        
        row++;
        worksheet.Cell(row, 1).Value = "Quality of Service:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.AverageQualityOfServiceScore;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
        
        row++;
        worksheet.Cell(row, 1).Value = "Coverage Availability:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.AverageCoverageAvailabilityScore;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";

        // Infrastructure Summary
        row += 2;
        worksheet.Cell(row, 1).Value = "Total Infrastructure";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        
        row++;
        worksheet.Cell(row, 1).Value = "Cell Towers:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.TotalCellTowers;
        
        row++;
        worksheet.Cell(row, 1).Value = "Fiber NAPs:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.TotalFiberNAPs;
        
        row++;
        worksheet.Cell(row, 1).Value = "WiFi Hotspots:";
        worksheet.Cell(row, 2).Value = reportData.ScoreSummary.TotalWifiHotspots;

        worksheet.Columns().AdjustToContents();
    }

    private void CreateConnectivityDetailWorksheet(XLWorkbook workbook, ConnectivityReportDto reportData)
    {
        var worksheet = workbook.Worksheets.Add("Detailed Data");

        // Headers
        int col = 1;
        worksheet.Cell(1, col++).Value = "Location";
        worksheet.Cell(1, col++).Value = "Population";
        worksheet.Cell(1, col++).Value = "Fiber Score";
        worksheet.Cell(1, col++).Value = "Internet Score";
        worksheet.Cell(1, col++).Value = "Tech Score";
        worksheet.Cell(1, col++).Value = "QoS Score";
        worksheet.Cell(1, col++).Value = "Coverage Score";
        worksheet.Cell(1, col++).Value = "Total Score";
        worksheet.Cell(1, col++).Value = "Cell Towers";
        worksheet.Cell(1, col++).Value = "Fiber NAPs";
        worksheet.Cell(1, col++).Value = "WiFi Hotspots";
        worksheet.Cell(1, col++).Value = "Providers";
        worksheet.Cell(1, col++).Value = "Dominant Provider";
        worksheet.Cell(1, col++).Value = "Avg Download (Mbps)";
        worksheet.Cell(1, col++).Value = "Avg Upload (Mbps)";
        worksheet.Cell(1, col++).Value = "Speed Tests";
        worksheet.Cell(1, col++).Value = "Last Assessed";

        // Style header
        var headerRange = worksheet.Range(1, 1, 1, col - 1);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        int row = 2;
        foreach (var area in reportData.Areas)
        {
            col = 1;
            worksheet.Cell(row, col++).Value = area.LocationName;
            worksheet.Cell(row, col++).Value = area.Population ?? 0;
            worksheet.Cell(row, col++).Value = area.FiberCoverageScore;
            worksheet.Cell(row, col++).Value = area.InternetAccessScore;
            worksheet.Cell(row, col++).Value = area.TechnologyTypeScore;
            worksheet.Cell(row, col++).Value = area.QualityOfServiceScore;
            worksheet.Cell(row, col++).Value = area.CoverageAvailabilityScore;
            worksheet.Cell(row, col++).Value = area.TotalScore;
            worksheet.Cell(row, col++).Value = area.CellTowerCount;
            worksheet.Cell(row, col++).Value = area.FiberNAPCount;
            worksheet.Cell(row, col++).Value = area.WifiHotspotCount;
            worksheet.Cell(row, col++).Value = area.ServiceProviderCount;
            worksheet.Cell(row, col++).Value = area.DominantServiceProvider ?? "";
            worksheet.Cell(row, col++).Value = area.AverageDownloadSpeedMbps ?? 0;
            worksheet.Cell(row, col++).Value = area.AverageUploadSpeedMbps ?? 0;
            worksheet.Cell(row, col++).Value = area.SpeedTestCount;
            worksheet.Cell(row, col++).Value = area.LastAssessed.ToString("yyyy-MM-dd");

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
    }
    #endregion
}