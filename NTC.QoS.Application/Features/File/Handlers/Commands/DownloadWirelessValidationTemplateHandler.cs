using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using System.Drawing;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

internal sealed class DownloadWirelessValidationTemplateHandler
    : IRequestHandler<DownloadWirelessValidationTemplateRequest, ApiResponse<Stream>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;

    public DownloadWirelessValidationTemplateHandler(
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
    }

    public async Task<ApiResponse<Stream>> Handle(DownloadWirelessValidationTemplateRequest request, CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();
        ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");

        try
        {
            using var package = new ExcelPackage();
            await GenerateWirelessValidationTemplate(package, request.MunicipalityId, cancellationToken);
            await package.SaveAsAsync(memoryStream, cancellationToken);
            if (memoryStream.Length == 0)
                throw new InvalidOperationException($"Excel file generation failed: The generated file is empty. MunicipalityId: {request.MunicipalityId}");
            memoryStream.Position = 0;
            return new ApiResponse<Stream>(memoryStream, true, "Wireless validation template generated successfully");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Excel file generation failed for MunicipalityId: {request.MunicipalityId}.", ex);
        }
    }

    private async Task GenerateWirelessValidationTemplate(ExcelPackage package, Guid municipalityId, CancellationToken cancellationToken)
    {
        var ws = package.Workbook.Worksheets.Add("Data");

        // Municipality (with province/region)
        var municipality = await GetMunicipalityWithLocationInfo(municipalityId, cancellationToken)
                         ?? throw new ArgumentException("Municipality not found");

        // Barangays & Providers
        var barangays = await GetBarangaysByMunicipality(municipalityId, cancellationToken);
        var providers = await GetWirelessServiceProviders(cancellationToken);

        barangays = barangays.OrderBy(b => b.Name).ToList();
        providers = providers.OrderBy(p => p).ToList();

        // Location header text
        var locationHeader = $"{municipality.Name}, {municipality.Province.Name}, {municipality.Province.Region.Name}";

        var groups = new[]
        {
            new { Generation = "2G", Technology = "GSM",     Bands = new[] { "B8-900", "B3-1800" } },
            new { Generation = "3G", Technology = "WCDMA",   Bands = new[] { "B5-850", "B8-900", "B1-2100" } },
            new { Generation = "4G", Technology = "FDD-LTE", Bands = new[] { "B28-700", "B5-850", "B8-900", "B3-1800", "B1-2100" } },
            new { Generation = "4G", Technology = "TDD-LTE", Bands = new[] { "B40-2300", "B41-2500", "B38-2600" } },
            new { Generation = "5G", Technology = "NR",      Bands = new[] { "B28-700", "B5-850", "B8-900", "B3-1800", "B1-2100", "B40-2300", "B41-2500", "B38-2600", "B78-3500" } }
        };

        // Build 4-row header and get last column used
        int lastCol = BuildWirelessHeader(ws, locationHeader, groups, startRow: 1, startCol: 1);

        var fixedProviderColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["GLOBE COMMUNICATIONS INC."] = ColorTranslator.FromHtml("#DDEBF7"),
            ["SMART COMMUNICATIONS INC."] = ColorTranslator.FromHtml("#E2EFDA"),
            ["DITO TELECOMMUNITY CORP."] = ColorTranslator.FromHtml("#FCE4D6"),
        };
        var fallbackPalette = new[]
        {
            ColorTranslator.FromHtml("#FFF2CC"),
            ColorTranslator.FromHtml("#EDEDED"),
            ColorTranslator.FromHtml("#E5DFEC"),
            ColorTranslator.FromHtml("#DAEEF3"),
        };
        var assignedColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        Color GetColorForProvider(string name)
        {
            if (fixedProviderColors.TryGetValue(name, out var c)) return c;
            if (!assignedColors.TryGetValue(name, out c))
            {
                c = fallbackPalette[assignedColors.Count % fallbackPalette.Length];
                assignedColors[name] = c;
            }
            return c;
        }

        // Data rows (Row 5+): each Barangay � each Provider
        int currentRow = 5;
        foreach (var barangay in barangays)
        {
            foreach (var provider in providers)
            {
                ws.Cells[currentRow, 1].Value = provider;
                ws.Cells[currentRow, 2].Value = barangay.Name;

                // Row background by provider
                var rowColor = GetColorForProvider(provider);
                using (var rng = ws.Cells[currentRow, 1, currentRow, lastCol])
                {
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(rowColor);
                }

                currentRow++;
            }
        }

        // Add hidden sheets for Provider and Barangay validation
        var providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        providerSheet.Hidden = eWorkSheetHidden.VeryHidden;
        var providerRange = providerSheet.Cells[1, 1, providers.Count, 1];
        AddDropdownValidation(ws, "A5:A1048576", providerRange);

        if (barangays.Count > 0)
        {
            var barangaySheet = package.Workbook.Worksheets.Add("Barangays");
            barangaySheet.Cells.LoadFromCollection(barangays.Select(b => new { Name = b.Name }));
            barangaySheet.Hidden = eWorkSheetHidden.VeryHidden;
            var barangayRange = barangaySheet.Cells[1, 1, barangays.Count, 1];
            AddDropdownValidation(ws, "B5:B1048576", barangayRange);
        }

        // Add hidden sheet and validation for all band columns (Cols 3 to lastCol) with 1, 0, TRUE, FALSE (blank allowed)
        List<string> bandValues = ["1", "0", "TRUE", "FALSE"];
        var bandSheet = package.Workbook.Worksheets.Add("BandValues");
        bandSheet.Cells.LoadFromCollection(bandValues.Select(v => new { Value = v }));
        bandSheet.Hidden = eWorkSheetHidden.VeryHidden;
        var bandRange = bandSheet.Cells[1, 1, bandValues.Count, 1];

        for (int col = 3; col <= lastCol; col++)
        {
            var colName = GetExcelColumnName(col);
            AddDropdownValidation(ws, $"{colName}5:{colName}1048576", bandRange, allowBlank: true);
        }

        // Draw light grid borders on all data cells
        int dataStartRow = 5;
        int dataEndRow = currentRow - 1;

        if (dataEndRow >= dataStartRow)
        {
            var grid = Color.FromArgb(210, 210, 210);
            var dataRange = ws.Cells[dataStartRow, 1, dataEndRow, lastCol];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Top.Color.SetColor(grid);
            dataRange.Style.Border.Bottom.Color.SetColor(grid);
            dataRange.Style.Border.Left.Color.SetColor(grid);
            dataRange.Style.Border.Right.Color.SetColor(grid);

            // Re-apply the barangay separators so they override the grid
            int blockSize = providers.Count;
            if (blockSize > 0)
            {
                for (int r = dataStartRow + blockSize - 1; r <= dataEndRow; r += blockSize)
                {
                    using (var rng = ws.Cells[r, 1, r, lastCol])
                    {
                        rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin; // or Medium
                        rng.Style.Border.Bottom.Color.SetColor(Color.FromArgb(170, 170, 170));
                    }
                }
            }
        }

        int techBorderTopRow = 2;
        int techBorderBottomRow = Math.Max(4, dataEndRow);
        var techGray = Color.FromArgb(160, 160, 160);

        // Recompute technology spans from the same 'groups' definition
        int colPtr = 3;
        foreach (var g in groups)
        {
            int techStart = colPtr;
            int techEnd = colPtr + g.Bands.Length - 1;

            using (var left = ws.Cells[techBorderTopRow, techStart, techBorderBottomRow, techStart])
            {
                left.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                left.Style.Border.Left.Color.SetColor(techGray);
            }

            using (var right = ws.Cells[techBorderTopRow, techEnd, techBorderBottomRow, techEnd])
            {
                right.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                right.Style.Border.Right.Color.SetColor(techGray);
            }

            colPtr = techEnd + 1;
        }

        // Freeze panes so header stays visible (row 5, col C)
        ws.View.FreezePanes(5, 3);

        AutoFitColumns(ws);
    }

    private static int BuildWirelessHeader(ExcelWorksheet ws, string locationHeader,
        IEnumerable<dynamic> groups, int startRow = 1, int startCol = 1)
    {
        int rowLoc = startRow;
        int rowGen = startRow + 1;
        int rowTech = startRow + 2;
        int rowBand = startRow + 3;

        // Row 4 fixed header
        ws.Cells[rowBand, startCol].Value = "Provider";
        ws.Cells[rowBand, startCol + 1].Value = "Barangay";

        // Build band columns starting at C
        int col = startCol + 2;

        // Track spans for generations and technologies across bands
        var genSpans = new List<(string Label, int Start, int End)>();
        var techSpans = new List<(string Label, int Start, int End)>();

        foreach (var g in groups)
        {
            int techStart = col;

            foreach (string band in g.Bands)
            {
                ws.Cells[rowBand, col].Value = band;
                col++;
            }

            int techEnd = col - 1;
            techSpans.Add(((string)g.Technology, techStart, techEnd));

            // Merge/extend generation span if consecutive groups share same generation
            if (genSpans.Count == 0 || genSpans[^1].Label != (string)g.Generation)
                genSpans.Add(((string)g.Generation, techStart, techEnd));
            else
                genSpans[^1] = (genSpans[^1].Label, genSpans[^1].Start, techEnd);
        }

        int lastCol = col - 1;

        // Row 1: Location across A..lastCol
        using (var rng = ws.Cells[rowLoc, startCol, rowLoc, lastCol])
        {
            rng.Merge = true;
            rng.Value = locationHeader;
            rng.Style.Font.Bold = true;
            rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            rng.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        // Row 2: "Generation" over A:B
        using (var rng = ws.Cells[rowGen, startCol, rowGen, startCol + 1])
        {
            rng.Merge = true;
            rng.Value = "Generation";
            rng.Style.Font.Bold = true;
            rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            rng.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }
        // Row 2: 2G/3G/4G/5G over band spans
        foreach (var (label, c1, c2) in genSpans)
        {
            using (var rng = ws.Cells[rowGen, c1, rowGen, c2])
            {
                rng.Merge = true;
                rng.Value = label;
                rng.Style.Font.Bold = true;
                rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                rng.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        // Row 3: "Technology" over A:B
        using (var rng = ws.Cells[rowTech, startCol, rowTech, startCol + 1])
        {
            rng.Merge = true;
            rng.Value = "Technology";
            rng.Style.Font.Bold = true;
            rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            rng.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }
        // Row 3: GSM/WCDMA/FDD-LTE/TDD-LTE/NR over band spans
        foreach (var (label, c1, c2) in techSpans)
        {
            using (var rng = ws.Cells[rowTech, c1, rowTech, c2])
            {
                rng.Merge = true;
                rng.Value = label;
                rng.Style.Font.Bold = true;
                rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                rng.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        // Style Row 4 (Provider, Barangay, Bands)
        for (int c = startCol; c <= lastCol; c++)
        {
            var cell = ws.Cells[rowBand, c];
            cell.Style.Font.Bold = true;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        return lastCol;
    }

    private async Task<Domain.Entities.Municipality?> GetMunicipalityWithLocationInfo(Guid municipalityId, CancellationToken cancellationToken)
    {
        var query = _municipalityRepository.Query(m => m.Id == municipalityId && !m.IsDeleted);
        return await _municipalityRepository.FirstOrDefaultAsync(query, cancellationToken);
    }

    private async Task<List<Domain.Entities.Barangay>> GetBarangaysByMunicipality(Guid municipalityId, CancellationToken cancellationToken)
    {
        var query = _barangayRepository.Query(b => b.MunicipalityId == municipalityId && !b.IsDeleted)
            .OrderBy(b => b.Name);

        return await _barangayRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<string>> GetWirelessServiceProviders(CancellationToken cancellationToken)
    {
        var query = _serviceProviderRepository.Query(sp => !sp.IsDeleted && sp.Service.Type == ServiceTypeEnum.Wireless)
            .OrderBy(sp => sp.Provider.Name)
            .Select(sp => sp.Provider.Name)
            .Distinct();

        return await _serviceProviderRepository.ToListAsync(query, cancellationToken);
    }

    private static void AutoFitColumns(ExcelWorksheet ws)
    {
        var dim = ws.Dimension;
        if (dim == null) return;
        ws.Cells[dim.Address].AutoFitColumns();
    }

    private static void AddDropdownValidation(ExcelWorksheet worksheet, string range, ExcelRangeBase sourceRange, bool allowBlank = false)
    {
        var validation = worksheet.DataValidations.AddListValidation(range);
        validation.AllowBlank = allowBlank;
        validation.ShowErrorMessage = true;
        validation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
        validation.ErrorTitle = "Invalid Selection";
        validation.Error = "The value entered does not match any preset option. Please choose from the dropdown menu.";
        validation.Formula.ExcelFormula = sourceRange.FullAddressAbsolute;

        validation.ShowInputMessage = true;
        validation.PromptTitle = "Preset Selection";
        validation.Prompt = "Please choose from the dropdown menu. If copy-pasting, ensure values match the allowed options exactly.";

        var firstCell = range.Split(':')[0];
        var cf = worksheet.ConditionalFormatting.AddExpression(new ExcelAddress(range));
        cf.Formula = $"AND({firstCell}<>\"\", ISERROR(MATCH({firstCell}, {sourceRange.FullAddressAbsolute}, 0)))";
        cf.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cf.Style.Fill.BackgroundColor.Color = Color.FromArgb(255, 199, 206);
        cf.Style.Font.Color.Color = Color.FromArgb(156, 0, 6);
    }

    private static string GetExcelColumnName(int columnNumber)
    {
        var columnName = new System.Text.StringBuilder();
        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnName.Insert(0, Convert.ToChar('A' + modulo));
            columnNumber = (columnNumber - modulo) / 26;
        }
        return columnName.ToString();
    }
}
