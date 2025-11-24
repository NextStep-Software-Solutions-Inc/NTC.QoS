using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Features.Reports.Dashboard.DTOs;
using NTC.QoS.Application.Features.Reports.Dashboard.Queries;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NTC.QoS.Application.Features.Reports.Dashboard.Handlers;

public class GetDashboardReportHandler : IRequestHandler<GetDashboardReportQuery, ApiResponse<DashboardReportResult>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetDashboardReportHandler> _logger;

    public GetDashboardReportHandler(IMediator mediator, ILogger<GetDashboardReportHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<DashboardReportResult>> Handle(GetDashboardReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Resolve Date Range
            var (startDate, endDate) = DateTimeHelper.ResolveDateRange(request.StartDate, request.EndDate, _logger, request.DaysInPast);

            // 2. Determine Scope
            Guid? targetId = null;
            LocationTypeEnum? locationType = null;
            string scopeName = "National";

            if (request.MunicipalityId.HasValue)
            {
                targetId = request.MunicipalityId.Value;
                locationType = LocationTypeEnum.Municipality;
                scopeName = "Municipality"; // Ideally fetch name
            }
            else if (request.ProvinceId.HasValue)
            {
                targetId = request.ProvinceId.Value;
                locationType = LocationTypeEnum.Provincial;
                scopeName = "Provincial";
            }
            else if (request.RegionId.HasValue)
            {
                targetId = request.RegionId.Value;
                locationType = LocationTypeEnum.Regional;
                scopeName = "Regional";
            }

            // 3. Fetch Data Sequentially (DbContext is not thread-safe)
            var cellCountResponse = await _mediator.Send(new GetTotalCellsitesCountRequest { StartDate = startDate, EndDate = endDate }, cancellationToken);
            var napCountResponse = await _mediator.Send(new GetTotalNAPsCountRequest { StartDate = startDate, EndDate = endDate }, cancellationToken);
            
            ApiResponse<List<GeoEntityCellCountDto>>? topAreasResponse = null;

            if (request.MunicipalityId.HasValue)
            {
                topAreasResponse = await _mediator.Send(new GetTopBarangaysInMunicipalityByCellCountRequest 
                { 
                    MunicipalityId = request.MunicipalityId, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    Limit = request.Limit 
                }, cancellationToken);
            }
            else if (request.ProvinceId.HasValue)
            {
                topAreasResponse = await _mediator.Send(new GetTopMunicipalitiesInProvinceByCellCountRequest 
                { 
                    ProvinceId = request.ProvinceId, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    Limit = request.Limit 
                }, cancellationToken);
            }
            else if (request.RegionId.HasValue)
            {
                topAreasResponse = await _mediator.Send(new GetTopProvincesInRegionByCellCountRequest 
                { 
                    RegionId = request.RegionId.Value, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    Limit = request.Limit 
                }, cancellationToken);
            }
            else
            {
                topAreasResponse = await _mediator.Send(new GetTopRegionsByCellCountRequest 
                { 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    Limit = request.Limit 
                }, cancellationToken);
            }

            ApiResponse<ConnectivityAnalysisResponseDto>? connectivityResponse = null;
            if (targetId.HasValue && locationType.HasValue)
            {
                connectivityResponse = await _mediator.Send(new GetConnectivityAnalysisRequest
                {
                    LocationIds = [targetId.Value],
                    LocationType = locationType.Value
                }, cancellationToken);
            }

            // 4. Prepare Data for Report
            var cellCount = cellCountResponse.Data?.TotalCells ?? 0;
            var napCount = napCountResponse.Data;
            var topAreas = topAreasResponse?.Data ?? [];
            var connectivityData = connectivityResponse?.Data?.Locations.FirstOrDefault();

            // Update scope name if we have connectivity data
            if (connectivityData != null)
            {
                scopeName = connectivityData.LocationName;
            }

            // 5. Generate PDF
            var pdfBytes = GeneratePdf(scopeName, startDate, endDate, cellCount, napCount, topAreas, connectivityData);

            return new ApiResponse<DashboardReportResult>(new DashboardReportResult
            {
                FileContent = pdfBytes,
                FileName = $"Dashboard_Report_{scopeName}_{DateTime.Now:yyyyMMdd}.pdf"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard report");
            return new ApiResponse<DashboardReportResult>("Failed to generate dashboard report.");
        }
    }

    private byte[] GeneratePdf(
        string scopeName, 
        DateTime? startDate, 
        DateTime? endDate, 
        int cellCount, 
        int napCount, 
        List<GeoEntityCellCountDto> topAreas, 
        LocationConnectivityDto? connectivity)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(header => 
                {
                    header.Column(col => 
                    {
                        col.Item().Row(row => 
                        {
                            row.RelativeItem().Column(c => 
                            {
                                c.Item().Text("NATIONAL TELECOMMUNICATIONS COMMISSION").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                                c.Item().Text("Quality of Service Management System").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("MMM dd, yyyy")).FontSize(10);
                        });
                        col.Item().PaddingVertical(10).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                        col.Item().AlignCenter().Text($"DASHBOARD & CONNECTIVITY REPORT").FontSize(18).Bold();
                        col.Item().AlignCenter().Text(scopeName.ToUpper()).FontSize(14).SemiBold().FontColor(Colors.Grey.Darken2);
                        
                        var dateRange = "All Time";
                        if (startDate.HasValue && endDate.HasValue)
                            dateRange = $"{startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}";
                        
                        col.Item().AlignCenter().Text($"Period: {dateRange}").FontSize(10).Italic();
                    });
                });

                page.Content().PaddingVertical(20).Column(col => 
                {
                    // 1. Executive Summary Cards
                    col.Item().Row(row => 
                    {
                        row.Spacing(10);
                        row.RelativeItem().Element(c => StatCard(c, "Total Cell Sites", cellCount.ToString("N0"), Colors.Blue.Lighten4, Colors.Blue.Darken2));
                        row.RelativeItem().Element(c => StatCard(c, "Total NAPs", napCount.ToString("N0"), Colors.Green.Lighten4, Colors.Green.Darken2));
                        
                        if (connectivity != null)
                        {
                            var (bgColor, textColor) = GetStatusColors(connectivity.ConnectivityStatus);
                            row.RelativeItem().Element(c => StatCard(c, "Connectivity Status", connectivity.ConnectivityStatus.ToString(), bgColor, textColor));
                        }
                    });

                    col.Item().PaddingVertical(20);

                    // 2. Connectivity Assessment Details
                    if (connectivity != null)
                    {
                        col.Item().Text("Connectivity Assessment").FontSize(14).Bold().FontColor(Colors.Black);
                        col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Row(row => 
                        {
                            row.RelativeItem().Column(c => 
                            {
                                c.Item().Text($"Total Score: {connectivity.TotalScore}/15").FontSize(12).Bold();
                                c.Item().PaddingTop(5);
                                c.Item().Table(table => 
                                {
                                    table.ColumnsDefinition(cd => 
                                    {
                                        cd.RelativeColumn();
                                        cd.ConstantColumn(50);
                                    });

                                    table.Cell().Text("Fiber Coverage");
                                    table.Cell().AlignRight().Text(connectivity.CMTSScores.FiberCoverage.ToString());
                                    
                                    table.Cell().Text("Internet Access");
                                    table.Cell().AlignRight().Text(connectivity.CMTSScores.InternetAccess.ToString());

                                    table.Cell().Text("Technology Type");
                                    table.Cell().AlignRight().Text(connectivity.CMTSScores.TechnologyType.ToString());

                                    table.Cell().Text("Quality of Service");
                                    table.Cell().AlignRight().Text(connectivity.CMTSScores.QualityOfService.ToString());

                                    table.Cell().Text("Coverage Availability");
                                    table.Cell().AlignRight().Text(connectivity.CMTSScores.CoverageAvailability.ToString());
                                });
                            });

                            row.ConstantItem(20);

                            row.RelativeItem().Column(c => 
                            {
                                c.Item().Text("Infrastructure").FontSize(12).Bold();
                                c.Item().PaddingTop(5);
                                c.Item().Table(table => 
                                {
                                    table.ColumnsDefinition(cd => 
                                    {
                                        cd.RelativeColumn();
                                        cd.ConstantColumn(50);
                                    });

                                    table.Cell().Text("Cell Towers");
                                    table.Cell().AlignRight().Text(connectivity.Infrastructure.CellTowerCount.ToString("N0"));

                                    table.Cell().Text("Fiber NAPs");
                                    table.Cell().AlignRight().Text(connectivity.Infrastructure.FiberNAPCount.ToString("N0"));

                                    table.Cell().Text("WiFi Hotspots");
                                    table.Cell().AlignRight().Text(connectivity.Infrastructure.WiFiHotspotCount.ToString("N0"));

                                    table.Cell().Text("Service Providers");
                                    table.Cell().AlignRight().Text(connectivity.Infrastructure.ServiceProviderCount.ToString("N0"));
                                });
                            });
                        });

                        col.Item().PaddingVertical(20);

                        // 2.1 Technology Profile
                        if (connectivity.Infrastructure.TechnologyBandBreakdown != null)
                        {
                            var tech = connectivity.Infrastructure.TechnologyBandBreakdown;
                            col.Item().Text("Technology Profile").FontSize(14).Bold().FontColor(Colors.Black);
                            col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            col.Item().Row(row => 
                            {
                                row.RelativeItem().Column(c => 
                                {
                                    c.Item().Text("Available Generations").FontSize(10).Bold();
                                    c.Item().PaddingTop(5).Row(r => 
                                    {
                                        r.Spacing(10);
                                        if (tech.Has2G) r.AutoItem().Text("2G").FontColor(Colors.Green.Darken2).Bold();
                                        if (tech.Has3G) r.AutoItem().Text("3G").FontColor(Colors.Green.Darken2).Bold();
                                        if (tech.Has4G) r.AutoItem().Text("4G").FontColor(Colors.Green.Darken2).Bold();
                                        if (tech.Has5G) r.AutoItem().Text("5G").FontColor(Colors.Green.Darken2).Bold();
                                    });
                                });

                                row.RelativeItem().Column(c => 
                                {
                                    c.Item().Text("Available Technologies").FontSize(10).Bold();
                                    if (tech.AvailableTechnologies != null && tech.AvailableTechnologies.Any())
                                    {
                                        c.Item().PaddingTop(5).Text(string.Join(", ", tech.AvailableTechnologies));
                                    }
                                    else
                                    {
                                        c.Item().PaddingTop(5).Text("None").FontColor(Colors.Grey.Medium);
                                    }
                                });
                            });
                            col.Item().PaddingVertical(10);
                        }

                        // 2.2 Provider Breakdown
                        if (connectivity.Infrastructure.ProviderBreakdown != null && connectivity.Infrastructure.ProviderBreakdown.Any())
                        {
                            col.Item().Text("Provider Infrastructure Breakdown").FontSize(14).Bold().FontColor(Colors.Black);
                            col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            col.Item().Table(table => 
                            {
                                table.ColumnsDefinition(cd => 
                                {
                                    cd.RelativeColumn(3); // Provider Name
                                    cd.RelativeColumn();  // Cell Towers
                                    cd.RelativeColumn();  // Fiber NAPs
                                    cd.RelativeColumn();  // Total Assets
                                    cd.RelativeColumn();  // Share %
                                });

                                // Header
                                table.Header(h => 
                                {
                                    h.Cell().Element(HeaderStyle).Text("Provider");
                                    h.Cell().Element(HeaderStyle).AlignRight().Text("Cell Towers");
                                    h.Cell().Element(HeaderStyle).AlignRight().Text("Fiber NAPs");
                                    h.Cell().Element(HeaderStyle).AlignRight().Text("Total Assets");
                                    h.Cell().Element(HeaderStyle).AlignRight().Text("Share %");
                                });

                                // Data
                                foreach (var provider in connectivity.Infrastructure.ProviderBreakdown)
                                {
                                    table.Cell().Element(CellStyle).Text(provider.ProviderName);
                                    table.Cell().Element(CellStyle).AlignRight().Text(provider.CellTowerCount.ToString("N0"));
                                    table.Cell().Element(CellStyle).AlignRight().Text(provider.FiberNAPCount.ToString("N0"));
                                    table.Cell().Element(CellStyle).AlignRight().Text(provider.TotalAssetCount.ToString("N0"));
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{provider.AssetPercentage:F2}%");
                                }

                                static IContainer HeaderStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                }

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                                }
                            });
                            col.Item().PaddingVertical(20);
                        }
                    }

                    // 3. Top Areas Chart
                    if (topAreas.Any())
                    {
                        col.Item().Text("Top Areas by Cell Count").FontSize(14).Bold().FontColor(Colors.Black);
                        col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Table(table => 
                        {
                            table.ColumnsDefinition(cd => 
                            {
                                cd.ConstantColumn(150); // Name
                                cd.RelativeColumn();    // Bar
                                cd.ConstantColumn(50);  // Value
                            });

                            var maxVal = topAreas.Max(x => x.CellCount);
                            if (maxVal == 0) maxVal = 1;

                            foreach (var area in topAreas)
                            {
                                table.Cell().AlignMiddle().Text(area.Name).FontSize(10);
                                
                                table.Cell().AlignMiddle().PaddingVertical(2).Row(r => 
                                {
                                    var percentage = (float)area.CellCount / maxVal;
                                    r.RelativeItem(percentage).Height(15).Background(Colors.Blue.Medium);
                                    r.RelativeItem(1 - percentage);
                                });

                                table.Cell().AlignMiddle().AlignRight().Text(area.CellCount.ToString("N0")).FontSize(10);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x => 
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        })
        .GeneratePdf();
    }

    private void StatCard(IContainer container, string label, string value, QuestPDF.Infrastructure.Color bgColor, QuestPDF.Infrastructure.Color textColor)
    {
        container
            .Background(bgColor)
            .Padding(10)
            .Column(c => 
            {
                c.Item().Text(label).FontSize(10).FontColor(Colors.Grey.Darken3);
                c.Item().Text(value).FontSize(20).Bold().FontColor(textColor);
            });
    }

    private (QuestPDF.Infrastructure.Color bg, QuestPDF.Infrastructure.Color text) GetStatusColors(ConnectivityStatusEnum status)
    {
        return status switch
        {
            ConnectivityStatusEnum.Served => (Colors.Green.Lighten4, Colors.Green.Darken2),
            ConnectivityStatusEnum.Underserved => (Colors.Orange.Lighten4, Colors.Orange.Darken2),
            ConnectivityStatusEnum.Unserved => (Colors.Red.Lighten4, Colors.Red.Darken2),
            _ => (Colors.Grey.Lighten4, Colors.Grey.Darken2)
        };
    }
}
