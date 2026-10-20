using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Infrastructure.Helper;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Color = System.Drawing.Color;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class ExportCellsHandler(
    ICellRepository cellRepository,
    IRepository<Domain.Entities.Cellular> cellularRepository,
    IRepository<Domain.Entities.Barangay> barangayRepository,
    IRepository<Domain.Entities.Municipality> municipalityRepository,
    IRepository<Domain.Entities.Province> provinceRepository,
    IRepository<Domain.Entities.Region> regionRepository,
    ILogger<ExportCellsHandler> logger) : IRequestHandler<ExportCellsRequest, ApiResponse<CellExportResultDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository = cellularRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository = barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository = municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository = provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;
    private readonly ILogger<ExportCellsHandler> _logger = logger;

    public async Task<ApiResponse<CellExportResultDto>> Handle(ExportCellsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cellQuery = _cellRepository.Query();

            var locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchExpression = SearchHelper.BuildNullableKeywordSearchExpression<Domain.Aggregates.Cell>(
                    request.Search,
                    c => c.Name,
                    c => c.Description,
                    c => c.Group,
                    c => c.Address,
                    c => c.SiteId
                );
                cellQuery = cellQuery.Where(searchExpression);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var statusEnum = EnumHelper<CellStatusEnum>.ParseUserInput(request.Status);
                cellQuery = cellQuery.Where(c => c.Status == statusEnum);
            }

            cellQuery = cellQuery
                .Where(c => request.ServiceProviderIds == null || request.ServiceProviderIds.Contains(c.ServiceProviderId))
                .Where(c => request.ProviderIds == null || (c.ServiceProvider != null && request.ProviderIds.Contains(c.ServiceProvider.ProviderId)))
                .Where(c => request.ServiceIds == null || (c.ServiceProvider != null && request.ServiceIds.Contains(c.ServiceProvider.ServiceId)))
                .Where(c => request.IsVerified == null || c.IsVerified == request.IsVerified.Value)
                .Where(c => locationPoint.IsValid && c.Location.IsWithinDistance(locationPoint, request.Distance));

            if (request.GenerationIds is { Count: > 0 } || request.TechnologyIds is { Count: > 0 } || request.BandIds is { Count: > 0 })
            {
                var cellularFilterQuery = _cellularRepository.QueryAsNoTracking();

                if (request.TechnologyIds is { Count: > 0 })
                {
                    cellularFilterQuery = cellularFilterQuery.Where(cellular =>
                        request.TechnologyIds.Contains(cellular.TechnologyId));
                }

                if (request.BandIds is { Count: > 0 })
                {
                    cellularFilterQuery = cellularFilterQuery.Where(cellular =>
                        request.BandIds.Contains(cellular.BandId));
                }

                if (request.GenerationIds is { Count: > 0 })
                {
                    cellularFilterQuery = cellularFilterQuery.Where(cellular =>
                        cellular.Technology != null &&
                        request.GenerationIds.Contains(cellular.Technology.GenerationId));
                }

                var cellIdsWithFilters = cellularFilterQuery
                    .Select(cellular => cellular.CellSector!.CellId)
                    .Distinct();

                cellQuery = cellQuery.Where(cell => cellIdsWithFilters.Contains(cell.Id));
            }

            if (request.BarangayIds is { Count: > 0 })
            {
                cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.BarangayIds, _barangayRepository, cellQuery, cancellationToken);
            }
            else if (request.MunicipalityIds is { Count: > 0 })
            {
                cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.MunicipalityIds, _municipalityRepository, cellQuery, cancellationToken);
            }
            else if (request.ProvinceIds is { Count: > 0 })
            {
                cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.ProvinceIds, _provinceRepository, cellQuery, cancellationToken);
            }
            else if (request.RegionIds is { Count: > 0 })
            {
                cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.RegionIds, _regionRepository, cellQuery, cancellationToken);
            }

            cellQuery = cellQuery
                .OrderBy(c => c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Name : string.Empty)
                .ThenBy(c => c.Name);

            var cellsProjection = await _cellRepository.ToListAsync(
                cellQuery.Select(cell => new
                {
                    cell.Id,
                    cell.Name,
                    cell.SiteId,
                    ServiceProviderName = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null
                        ? cell.ServiceProvider.Provider.Name
                        : "Unknown",
                    Address = cell.Address ?? string.Empty,
                    Latitude = cell.Location.Coordinate.Y,
                    Longitude = cell.Location.Coordinate.X,
                    Status = cell.Status.ToString(),
                    cell.IsVerified
                }),
                cancellationToken);

            // Fetch technology and band combinations for the matched cells
            var cellIds = cellsProjection.Select(c => c.Id).ToList();
            var techBandMap = new Dictionary<Guid, string>();

            if (cellIds.Count > 0)
            {
                const int batchSize = 2000;
                var allCellularRows = new List<(Guid CellId, string TechStandard, string BandName)>();

                for (int i = 0; i < cellIds.Count; i += batchSize)
                {
                    var batchIds = cellIds.Skip(i).Take(batchSize).ToList();
                    var cellularQuery = _cellularRepository.QueryAsNoTracking()
                        .Where(c => c.CellSector != null && batchIds.Contains(c.CellSector.CellId))
                        .Select(c => new
                        {
                            CellId = c.CellSector!.CellId,
                            TechStandard = c.Technology != null ? c.Technology.Standard : string.Empty,
                            BandName = c.Band != null ? c.Band.Name : string.Empty
                        })
                        .Distinct();

                    var batchRows = await _cellularRepository.ToListAsync(cellularQuery, cancellationToken);
                    foreach (var row in batchRows)
                    {
                        allCellularRows.Add((row.CellId, row.TechStandard, row.BandName));
                    }
                }

                techBandMap = allCellularRows
                    .GroupBy(r => r.CellId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var byTech = g
                                .Where(x => !string.IsNullOrWhiteSpace(x.TechStandard) || !string.IsNullOrWhiteSpace(x.BandName))
                                .GroupBy(x => string.IsNullOrWhiteSpace(x.TechStandard) ? "Unknown" : x.TechStandard)
                                .OrderBy(tg => tg.Key)
                                .Select(tg =>
                                {
                                    var bands = tg
                                        .Select(x => x.BandName)
                                        .Where(b => !string.IsNullOrWhiteSpace(b))
                                        .Distinct()
                                        .OrderBy(b => b)
                                        .ToList();

                                    return bands.Count > 0
                                        ? $"{tg.Key} ({string.Join(", ", bands)})"
                                        : tg.Key;
                                });

                            var formatted = string.Join("; ", byTech);
                            return string.IsNullOrWhiteSpace(formatted) ? "N/A" : formatted;
                        });
            }

            var exportItems = cellsProjection.Select(c => new CellExportItemDto
            {
                Id = c.Id,
                Name = string.IsNullOrWhiteSpace(c.Name) ? $"Cell {c.Id}" : c.Name,
                SiteId = string.IsNullOrWhiteSpace(c.SiteId) ? "N/A" : c.SiteId,
                ServiceProviderName = c.ServiceProviderName,
                Address = string.IsNullOrWhiteSpace(c.Address) ? "N/A" : c.Address,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Status = c.Status,
                IsVerified = c.IsVerified,
                TechnologyBands = techBandMap.GetValueOrDefault(c.Id, "N/A")
            }).ToList();

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            byte[] fileContent;
            string contentType;
            string fileName;

            if (request.Format == ReportFormat.Pdf)
            {
                fileContent = GeneratePdf(exportItems);
                contentType = "application/pdf";
                fileName = $"CellSites_Export_{timestamp}.pdf";
            }
            else
            {
                fileContent = GenerateExcel(exportItems);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"CellSites_Export_{timestamp}.xlsx";
            }

            return new ApiResponse<CellExportResultDto>
            {
                Succeeded = true,
                Message = "Cell sites exported successfully.",
                Data = new CellExportResultDto
                {
                    FileContent = fileContent,
                    ContentType = contentType,
                    FileName = fileName
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting cell sites.");
            return new ApiResponse<CellExportResultDto>
            {
                Succeeded = false,
                Message = $"Failed to export cell sites: {ex.Message}"
            };
        }
    }

    private static byte[] GenerateExcel(List<CellExportItemDto> items)
    {
        ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Cell Sites");

        var headers = new[]
        {
            "No.",
            "Cell Site Name",
            "Site ID",
            "Service Provider",
            "Latitude",
            "Longitude",
            "Technology & Band",
            "Address",
            "Status",
            "Verified"
        };

        // Title Row
        worksheet.Cells[1, 1, 1, headers.Length].Merge = true;
        worksheet.Cells[1, 1].Value = "CELL SITE MANAGEMENT REPORT";
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;
        worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(37, 99, 235));
        worksheet.Cells[1, 1].Style.Font.Color.SetColor(Color.White);

        // Subtitle Row
        worksheet.Cells[2, 1, 2, headers.Length].Merge = true;
        worksheet.Cells[2, 1].Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | Total Cell Sites: {items.Count}";
        worksheet.Cells[2, 1].Style.Font.Size = 10;
        worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Headers Row (Row 3)
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cells[3, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(226, 232, 240));
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        // Data Rows
        int row = 4;
        int index = 1;
        foreach (var item in items)
        {
            worksheet.Cells[row, 1].Value = index++;
            worksheet.Cells[row, 2].Value = item.Name;
            worksheet.Cells[row, 3].Value = item.SiteId;
            worksheet.Cells[row, 4].Value = item.ServiceProviderName;
            worksheet.Cells[row, 5].Value = Math.Round(item.Latitude, 6);
            worksheet.Cells[row, 6].Value = Math.Round(item.Longitude, 6);
            worksheet.Cells[row, 7].Value = item.TechnologyBands;
            worksheet.Cells[row, 8].Value = item.Address;
            worksheet.Cells[row, 9].Value = item.Status;
            worksheet.Cells[row, 10].Value = item.IsVerified ? "Verified" : "Unverified";

            for (int col = 1; col <= headers.Length; col++)
            {
                worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 5].Style.Numberformat.Format = "0.000000";
            worksheet.Cells[row, 6].Style.Numberformat.Format = "0.000000";
            worksheet.Cells[row, 7].Style.WrapText = true;
            worksheet.Cells[row, 8].Style.WrapText = true;
            worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            row++;
        }

        worksheet.Cells.AutoFitColumns(10, 50);
        worksheet.View.FreezePanes(4, 1);

        return package.GetAsByteArray();
    }

    private static byte[] GeneratePdf(List<CellExportItemDto> items)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("NATIONAL TELECOMMUNICATIONS COMMISSION")
                        .FontSize(14).Bold().FontColor(Colors.Grey.Darken3);

                    column.Item().AlignCenter().Text("Quality of Service Management System")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);

                    column.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Darken2);

                    column.Item().AlignCenter().Text("Cell Site Management Export Report")
                        .FontSize(12).Bold().FontColor(Colors.Blue.Darken2);

                    column.Item().PaddingBottom(6).AlignCenter()
                        .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | Total Cell Sites: {items.Count}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(28);  // No.
                        columns.RelativeColumn(2.0f); // Cell Site Name
                        columns.RelativeColumn(1.2f); // Site ID
                        columns.RelativeColumn(1.5f); // Service Provider
                        columns.ConstantColumn(58);  // Latitude
                        columns.ConstantColumn(58);  // Longitude
                        columns.RelativeColumn(2.8f); // Technology & Band
                        columns.RelativeColumn(2.0f); // Address
                        columns.ConstantColumn(52);  // Status
                        columns.ConstantColumn(52);  // Verified
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).AlignCenter().Text("No.");
                        header.Cell().Element(HeaderStyle).Text("Cell Site Name");
                        header.Cell().Element(HeaderStyle).Text("Site ID");
                        header.Cell().Element(HeaderStyle).Text("Service Provider");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Latitude");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Longitude");
                        header.Cell().Element(HeaderStyle).Text("Technology & Band");
                        header.Cell().Element(HeaderStyle).Text("Address");
                        header.Cell().Element(HeaderStyle).AlignCenter().Text("Status");
                        header.Cell().Element(HeaderStyle).AlignCenter().Text("Verified");
                    });

                    int index = 1;
                    foreach (var item in items)
                    {
                        var bgColor = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                        table.Cell().Element(c => DataCellStyle(c, bgColor)).AlignCenter().Text(index.ToString());
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(item.Name);
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(item.SiteId);
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(item.ServiceProviderName);
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).AlignRight().Text(item.Latitude.ToString("F6"));
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).AlignRight().Text(item.Longitude.ToString("F6"));
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(item.TechnologyBands);
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(item.Address);
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).AlignCenter().Text(item.Status);
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).AlignCenter().Text(item.IsVerified ? "Verified" : "Unverified");

                        index++;
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignLeft()
                        .Text("NTC QoS - Cell Site Coordinates & Technology Band Report")
                        .FontSize(7).FontColor(Colors.Grey.Medium);

                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }).GeneratePdf();

        static IContainer HeaderStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(Colors.Blue.Darken2)
                .PaddingVertical(4)
                .PaddingHorizontal(4)
                .AlignMiddle()
                .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8));
        }

        static IContainer DataCellStyle(IContainer container, string backgroundColor)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(backgroundColor)
                .PaddingVertical(3)
                .PaddingHorizontal(4)
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontSize(7.5f));
        }
    }
}
