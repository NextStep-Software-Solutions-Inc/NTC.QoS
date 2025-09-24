using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

internal sealed class DownloadWirelessValidationTemplateHandler : IRequestHandler<DownloadWirelessValidationTemplateRequest, ApiResponse<Stream>>
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

        using var package = new ExcelPackage();
        await GenerateWirelessValidationTemplate(package, request.MunicipalityId, cancellationToken);

        await package.SaveAsAsync(memoryStream, cancellationToken);
        if (memoryStream.Length == 0)
        {
            throw new InvalidOperationException("Excel file generation failed.");
        }
        memoryStream.Position = 0;

        return new ApiResponse<Stream>(memoryStream, succeeded: true, message: "Wireless validation template generated successfully");
    }

    private async Task GenerateWirelessValidationTemplate(ExcelPackage package, Guid municipalityId, CancellationToken cancellationToken)
    {
        var worksheet = package.Workbook.Worksheets.Add("Data");
        
        // Get municipality with province and region info
        var municipality = await GetMunicipalityWithLocationInfo(municipalityId, cancellationToken);
        if (municipality is null)
        {
            throw new ArgumentException("Municipality not found");
        }

        // Get barangays for the municipality
        var barangays = await GetBarangaysByMunicipality(municipalityId, cancellationToken);
        
        // Get wireless service providers
        var providers = await GetWirelessServiceProviders(cancellationToken);

        // Create the location header
        var locationHeader = $"{municipality.Name}, {municipality.Province.Name}, {municipality.Province.Region.Name}";

        // Define the multi-row header data (4 rows, proper structure)
        object[,] headers = new object[,]
        {
            // Row 1: Location header spanning all columns
            { locationHeader, "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" },
            // Row 2: Generation headers (Provider and Barangay will be merged vertically)
            { "", "", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "2G", "2G", "3G", "3G", "3G", "4G", "4G", "4G", "4G", "4G", "4G", "4G", "4G", "5G" },
            // Row 3: Technology headers (Provider and Barangay will be merged vertically)  
            { "", "", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "GSM", "GSM", "WCDMA", "WCDMA", "WCDMA", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "TDD-LTE", "TDD-LTE", "TDD-LTE", "NR" },
            // Row 4: Column headers (Provider, Barangay, Bands)
            { "Provider", "Barangay", "B8-900", "B3-1800", "B5-850", "B8-900", "B1-2100", "B28-700", "B5-850", "B8-900", "B3-1800", "B1-2100", "B40-2300", "B41-2500", "B38-2600", "B3-1800", "B1-2100", "B40-2300", "B41-2500", "B38-2600", "Bn78-3500", "" }
        };

        // Apply the multi-row headers
        AddWirelessValidationMultiRowHeaders(worksheet, headers, 1);

        // Add instruction/legend about validation values
        worksheet.Cells["A1"].AddComment("Instructions:\n- TRUE: Band is present\n- FALSE: Band is not present\n- MARGINAL: Band has marginal coverage\n- BLANK: Same as FALSE (band not present)", "NTC QoS System");

        // Generate data rows - repeat barangays for each service provider
        int currentRow = 5; // Start after 4 header rows
        
        foreach (var provider in providers)
        {
            foreach (var barangay in barangays)
            {
                worksheet.Cells[currentRow, 1].Value = provider; // Provider
                worksheet.Cells[currentRow, 2].Value = barangay.Name; // Barangay
                
                currentRow++;
            }
        }

        // Add validation options (TRUE, FALSE, MARGINAL) - blank is also allowed and treated as FALSE
        string[] validationValues = ["TRUE", "FALSE", "MARGINAL"];
        
        // Apply dropdown validation to band columns (C to U) for all data rows
        int totalRows = providers.Count * barangays.Count;
        int lastDataRow = 4 + totalRows;
        
        for (int col = 3; col <= 21; col++) // Columns C through U (band columns)
        {
            string columnLetter = GetColumnLetter(col);
            string range = $"{columnLetter}5:{columnLetter}{lastDataRow}";
            AddDropdownValidation(worksheet, range, validationValues.ToList());
        }

        // Add providers to a hidden sheet for provider validation
        var providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        providerSheet.Hidden = eWorkSheetHidden.Hidden;
        
        // Add provider validation
        string providerRange = $"A5:A{lastDataRow}";
        AddDropdownValidation(worksheet, providerRange, providerSheet.Cells[1, 1, providers.Count, 1]);

        AutoFitColumns(worksheet);
    }

    private static void AddWirelessValidationMultiRowHeaders(ExcelWorksheet worksheet, object[,] headers, int startRow)
    {
        int numRows = headers.GetLength(0);
        int numCols = headers.GetLength(1);

        // Write all header values first
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                var cell = worksheet.Cells[startRow + row, col + 1];
                cell.Value = headers[row, col];
                cell.Style.Font.Bold = true;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        // Row 1: Location header - merge across all columns
        using (var range = worksheet.Cells[startRow, 1, startRow, numCols])
        {
            range.Merge = true;
        }

        // Row 2: Generation merging - don't overlap with Provider/Barangay columns
        using (var range = worksheet.Cells[startRow + 1, 3, startRow + 1, 8]) // Start from column 3 (C)
        {
            range.Merge = true;
            range.Value = "Generation";
        }
        using (var range = worksheet.Cells[startRow + 1, 9, startRow + 1, 10])
        {
            range.Merge = true;
            range.Value = "2G";
        }
        using (var range = worksheet.Cells[startRow + 1, 11, startRow + 1, 13])
        {
            range.Merge = true;
            range.Value = "3G";
        }
        using (var range = worksheet.Cells[startRow + 1, 14, startRow + 1, 21])
        {
            range.Merge = true;
            range.Value = "4G";
        }
        using (var range = worksheet.Cells[startRow + 1, 22, startRow + 1, 22])
        {
            range.Value = "5G";
        }

        // Row 3: Technology merging - don't overlap with Provider/Barangay columns
        using (var range = worksheet.Cells[startRow + 2, 3, startRow + 2, 8]) // Start from column 3 (C)
        {
            range.Merge = true;
            range.Value = "Technology";
        }
        using (var range = worksheet.Cells[startRow + 2, 9, startRow + 2, 10])
        {
            range.Merge = true;
            range.Value = "GSM";
        }
        using (var range = worksheet.Cells[startRow + 2, 11, startRow + 2, 13])
        {
            range.Merge = true;
            range.Value = "WCDMA";
        }
        using (var range = worksheet.Cells[startRow + 2, 14, startRow + 2, 18])
        {
            range.Merge = true;
            range.Value = "FDD-LTE";
        }
        using (var range = worksheet.Cells[startRow + 2, 19, startRow + 2, 21])
        {
            range.Merge = true;
            range.Value = "TDD-LTE";
        }
        using (var range = worksheet.Cells[startRow + 2, 22, startRow + 2, 22])
        {
            range.Value = "NR";
        }

        // Merge Provider and Barangay vertically for rows 2-4 (avoid the location header)
        using (var range = worksheet.Cells[startRow + 1, 1, startRow + 3, 1])
        {
            range.Merge = true;
            range.Value = "Provider";
        }
        using (var range = worksheet.Cells[startRow + 1, 2, startRow + 3, 2])
        {
            range.Merge = true;
            range.Value = "Barangay";
        }

        // Freeze the header rows
        worksheet.View.FreezePanes(startRow + numRows, 1);
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

    private static void AddDropdownValidation(ExcelWorksheet worksheet, string range, ExcelRangeBase sourceRange, string? defaultValue = null)
    {
        var validation = worksheet.DataValidations.AddListValidation(range);
        validation.AllowBlank = false;
        validation.ShowErrorMessage = true;
        validation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
        validation.ErrorTitle = "Invalid input";
        validation.Error = "Choose only from the dropdown menu";
        validation.Formula.ExcelFormula = sourceRange.FullAddressAbsolute;

        if (!string.IsNullOrEmpty(defaultValue))
        {
            string cellAddress = range.Split(':')[0];
            ExcelRange targetCell = worksheet.Cells[cellAddress];
            targetCell.Value = defaultValue;
            targetCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
    }

    private static void AddDropdownValidation(ExcelWorksheet worksheet, string range, List<string> values, string? defaultValue = null)
    {
        var validation = worksheet.DataValidations.AddListValidation(range);
        validation.AllowBlank = true;
        validation.ShowErrorMessage = true;
        validation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
        validation.ErrorTitle = "Invalid input";
        validation.Error = "Choose from: TRUE, FALSE, MARGINAL, or leave blank (blank = FALSE)";

        foreach (var value in values)
        {
            validation.Formula.Values.Add(value);
        }

        if (!string.IsNullOrEmpty(defaultValue))
        {
            string cellAddress = range.Split(':')[0];
            ExcelRange targetCell = worksheet.Cells[cellAddress];
            targetCell.Value = defaultValue;
            targetCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
    }

    private static void AutoFitColumns(ExcelWorksheet worksheet)
    {
        for (int i = 1; i <= worksheet.Dimension.Columns; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private static string GetColumnLetter(int columnNumber)
    {
        string columnName = "";
        
        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            columnNumber = (columnNumber - modulo) / 26;
        }
        
        return columnName;
    }
}