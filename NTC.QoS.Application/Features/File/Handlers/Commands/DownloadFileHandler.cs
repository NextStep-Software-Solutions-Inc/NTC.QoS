using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using System.Linq.Expressions;


namespace NTC.QoS.Application.Features.File.Handlers.Commands;

internal sealed class DownloadFileHandler : IRequestHandler<DownloadTemplateFileRequest, ApiResponse<Stream>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Domain.Aggregates.Generation> _generationRepository;
    private readonly IRepository<Domain.Entities.Technology> _technologyRepository;
    private readonly IRepository<Domain.Entities.Band> _bandRepository;
    private readonly IRepository<Domain.Aggregates.Network> _networkRepository;

    public DownloadFileHandler(
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Aggregates.Generation> generationRepository,
        IRepository<Domain.Entities.Technology> technologyRepository,
        IRepository<Domain.Entities.Band> bandRepository,
        IRepository<Domain.Aggregates.Network> networkRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _generationRepository = generationRepository;
        _technologyRepository = technologyRepository;
        _bandRepository = bandRepository;
        _networkRepository = networkRepository;
    }

    public async Task<ApiResponse<Stream>> Handle(DownloadTemplateFileRequest request, CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();
        ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");

        using var package = new ExcelPackage();
        var fileType = Enum.Parse<FileTypeEnum>(request.Type);
        
        // Use a switch expression to handle different file types
        switch (fileType)
        {
            case FileTypeEnum.NAP:
                await GenerateTemplate(package, worksheet => GenerateNAPTemplate(package, worksheet, cancellationToken), cancellationToken);
                break;

            case FileTypeEnum.Speedtest:
                await GenerateTemplate(package, worksheet => GenerateSpeedtestTemplate(package, worksheet, cancellationToken), cancellationToken);
                break;

            case FileTypeEnum.Cell:
                await GenerateTemplate(package, worksheet => GenerateCellTemplate(package, worksheet, cancellationToken), cancellationToken);
                break;

            case FileTypeEnum.CellularCoverage:
                await GenerateTemplate(package, worksheet => GenerateCellularCoverageTemplate(package, worksheet, cancellationToken), cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request.Type), $"Unsupported file type: {request.Type}");
        }

        await package.SaveAsAsync(memoryStream, cancellationToken);
        if (memoryStream.Length == 0)
        {
            throw new InvalidOperationException("Excel file generation failed.");
        }
        memoryStream.Position = 0;

        return new ApiResponse<Stream>(memoryStream, succeeded: true, message: "File generated successfully");
    }

    private async Task GenerateTemplate(ExcelPackage package, Func<ExcelWorksheet, Task> TemplateGenerator, CancellationToken cancellationToken)
    {
        var worksheet = package.Workbook.Worksheets.Add("Data");
        await TemplateGenerator(worksheet);
        AutoFitColumns(worksheet);
    }

    // Just duplicate and modify this method for more template
    private async Task GenerateNAPTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddCommonHeaders(worksheet, "Provider", "Barangay", "Municipality", "Province", "Region", "Lat", "Lng");

        var providers = await GetServiceProviders(cancellationToken);

        // Add providers to a hidden sheet
        var providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        var providerRange = providerSheet.Cells[1, 1, providers.Count, 1];
        providerSheet.Hidden = eWorkSheetHidden.Hidden;

        AddDropdownValidation(worksheet, "A2:A10", providerRange);
    }

    private async Task GenerateCellTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        // Define the multi-row header data
        object[,] headers = new object[,]
        {
            { "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "2G", "2G", "3G", "3G", "3G", "4G", "4G", "4G","4G", "4G", "4G", "4G", "4G", "5G", "5G", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (°)", "Beam Tilt (°)"},
            { "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "GSM", "GSM", "WCDMA", "WCDMA", "WCDMA", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "TDD-LTE", "TDD-LTE", "TDD-LTE", "", "", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (°)", "Beam Tilt (°)"},
            { "Provider", "Zone", "Barangay", "Municipality", "Province", "Region", "Lat", "Long", "900", "1800", "850", "900", "2100", "700", "850", "900", "1800", "2100", "2300", "2500", "2600", "2600", "3500", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (°)", "Beam Tilt (°)" }
        };

        // Apply the multi-row headers
        AddCellMultiRowHeaders(worksheet, headers, 1); // Start writing from row 1

        var providers = await GetServiceProviders(cancellationToken);
        var towerTypes = Enum.GetNames(typeof(TowerTypeEnum)).ToList();
        var antennaTypes = Enum.GetNames(typeof(AntennaTypeEnum)).ToList();
        string[] trueFalseValues = ["TRUE", "FALSE"];

        // Add hidden sheets for dropdowns
        ExcelWorksheet providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        providerSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "A4:A14", providerSheet.Cells[1, 1, providers.Count, 1]);

        ExcelWorksheet towerTypeSheet = package.Workbook.Worksheets.Add("TowerTypes");
        towerTypeSheet.Cells.LoadFromCollection(towerTypes.Select(t => new { Type = t }));
        towerTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "X4:X14", towerTypeSheet.Cells[1, 1, towerTypes.Count, 1]);

        ExcelWorksheet antennaTypeSheet = package.Workbook.Worksheets.Add("AntennaTypes");
        antennaTypeSheet.Cells.LoadFromCollection(antennaTypes.Select(a => new { Type = a }));
        antennaTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "Y4:Y14", antennaTypeSheet.Cells[1, 1, antennaTypes.Count, 1]);

        // Add "True/False" dropdowns directly
        AddDropdownValidation(worksheet, "I4:I14", [.. trueFalseValues]); // "2G GSM 900"
        AddDropdownValidation(worksheet, "J4:J14", [.. trueFalseValues]); // "2G GSM 1800"
        AddDropdownValidation(worksheet, "K4:K14", [.. trueFalseValues]); // "3G WCDMA 850"
        AddDropdownValidation(worksheet, "L4:L14", [.. trueFalseValues]); // "3G WCDMA 900"
        AddDropdownValidation(worksheet, "M4:M14", [.. trueFalseValues]); // "3G WCDMA 2100"
        AddDropdownValidation(worksheet, "N4:N14", [.. trueFalseValues]); // "4G FDD-LTE 700"
        AddDropdownValidation(worksheet, "O4:O14", [.. trueFalseValues]); // "4G FDD-LTE 850"
        AddDropdownValidation(worksheet, "P4:P14", [.. trueFalseValues]); // "4G FDD-LTE 900"
        AddDropdownValidation(worksheet, "Q4:Q14", [.. trueFalseValues]); // "4G FDD-LTE 1800"
        AddDropdownValidation(worksheet, "R4:R14", [.. trueFalseValues]); // "4G FDD-LTE 2100"
        AddDropdownValidation(worksheet, "S4:S14", [.. trueFalseValues]); // "4G TDD-LTE 2300"
        AddDropdownValidation(worksheet, "T4:T14", [.. trueFalseValues]); // "4G TDD-LTE 2500"
        AddDropdownValidation(worksheet, "U4:U14", [.. trueFalseValues]); // "4G TDD-LTE 2600"
        AddDropdownValidation(worksheet, "V4:V14", [.. trueFalseValues]); // "5G 2600"
        AddDropdownValidation(worksheet, "W4:W14", [.. trueFalseValues]); // "5G 3500"
    }

    private async Task GenerateSpeedtestTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddCommonHeaders(worksheet, "timestamp", "ping", "jitter", "upload", "download", "region", "province", "municipality", "barangay", "rssi", "signal_quality", "operator", "lat", "lon", "phone_model", "email", "nro", "network_type", "cell_id", "MCC", "MNC", "tac");

        var providers = await GetWirelessServiceProviders(cancellationToken);
        var technologies = await GetTechnologies(cancellationToken);
        var signalQuality = Enum.GetNames(typeof(SignalQualityEnum)).ToList();

        AddDropdownValidation(worksheet, "L2:L10", providers);
        AddDropdownValidation(worksheet, "R2:R10", technologies);
        AddDropdownValidation(worksheet, "K2:K10", signalQuality);
    }

    private async Task GenerateCellularCoverageTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddCommonHeaders(worksheet, "Network", "Lat", "Lng", "Strength", "Address");

        var networks = await GetNetworks(cancellationToken);
        AddDropdownValidation(worksheet, "A2:A12", networks);

        worksheet.Cells.AutoFitColumns();
    }

    private static void AddCommonHeaders(ExcelWorksheet worksheet, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }
    }

    private static void AddCellMultiRowHeaders(ExcelWorksheet worksheet, object[,] headers, int startRow)
    {
        int numRows = headers.GetLength(0);
        int numCols = headers.GetLength(1);

        // Write all header values with basic styling
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

        // Merge "Generation" across the initial columns
        using (var range = worksheet.Cells[startRow, 1, startRow, 8])
        {
            range.Merge = true;
        }

        // Merge "Technology" across the initial columns
        using (var range = worksheet.Cells[startRow + 1, 1, startRow + 1, 8])
        {
            range.Merge = true;
        }

        // Define groups for merging (starting from column 9)
        var generationGroups = new List<(int Start, int End)>
        {
            (9, 10), // 2G
            (11, 13), // 3G
            (14, 21), // 4G
            (22, 23) // 5G
        };

        var technologyGroups = new List<(int Start, int End)>
        {
            (9, 10), // GSM
            (11, 13), // WCDMA
            (14, 18), // FDD-LTE
            (19, 21), // TDD-LTE
            (22, 23) // 5G
        };

        // Apply merging for "Generation"
        foreach (var group in generationGroups)
        {
            using var range = worksheet.Cells[startRow, group.Start, startRow, group.End];
            range.Merge = true;
        }

        // Apply merging for "Technology"
        foreach (var group in technologyGroups)
        {
            using var range = worksheet.Cells[startRow + 1, group.Start, startRow + 1, group.End];
            range.Merge = true;
        }

        // Merge the last three columns vertically
        using (var range = worksheet.Cells[startRow, 24, startRow + 2, 24]) range.Merge = true; // Tower Type
        using (var range = worksheet.Cells[startRow, 25, startRow + 2, 25]) range.Merge = true; // Antenna Type
        using (var range = worksheet.Cells[startRow, 26, startRow + 2, 26]) range.Merge = true; // Antenna Height
        using (var range = worksheet.Cells[startRow, 27, startRow + 2, 27]) range.Merge = true; // Azimuth
        using (var range = worksheet.Cells[startRow, 28, startRow + 2, 28]) range.Merge = true; // Beam Tilt

        // Freeze the header rows
        worksheet.View.FreezePanes(startRow + numRows, 1);
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
        validation.AllowBlank = false;
        validation.ShowErrorMessage = true;
        validation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
        validation.ErrorTitle = "Invalid input";
        validation.Error = "Choose only from the dropdown menu";

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

    private async Task<List<string>> GetServiceProviders(CancellationToken cancellationToken)
    {
        var query = _serviceProviderRepository.Query(sp => !sp.IsDeleted && sp.Service.Type == ServiceTypeEnum.Wired)
            .OrderBy(sp => sp.Provider.Name)
            .Select(sp => sp.Provider.Name)
            .Distinct();

        return await _serviceProviderRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<string>> GetGenerations(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_generationRepository, g => !g.IsDeleted, g => g.Name, cancellationToken);
    }

    private async Task<List<string>> GetTechnologies(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_technologyRepository, t => !t.IsDeleted, t => t.Standard, cancellationToken);
    }

    private async Task<List<string>> GetBands(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_bandRepository, b => !b.IsDeleted, b => b.Name, cancellationToken);
    }

    private async Task<List<string>> GetWirelessServiceProviders(CancellationToken cancellationToken)
    {
        var query = _serviceProviderRepository.Query(sp => !sp.IsDeleted && sp.Service.Type == ServiceTypeEnum.Wireless)
            .OrderBy(sp => sp.Provider.Name)
            .Select(sp => sp.Provider.Name);

        return await _serviceProviderRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<string>> GetNetworks(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_networkRepository, n => !n.IsDeleted, n => n.Name, cancellationToken);
    }

    private static async Task<List<string>> GetEntityNames<TEntity>(
        IRepository<TEntity> repository,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, string>> selector,
        CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var query = repository.Query(predicate).Select(selector).Distinct().OrderBy(name => name);
        return await repository.ToListAsync(query, cancellationToken);
    }
}
