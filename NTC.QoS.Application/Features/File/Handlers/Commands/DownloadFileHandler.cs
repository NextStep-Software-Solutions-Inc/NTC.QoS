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
    // Speedtest Template Headers - define once for consistency
    private static readonly string[] SpeedtestHeaders = 
    [
        "timestamp", "ping", "jitter", "upload", "download", "region", "province", 
        "municipality", "barangay", "rssi", "signal_quality", "operator", "lat", 
        "lon", "phone_model", "email", "nro", "network_type", "cell_id", "MCC", "MNC", "tac",
        "success", "nbc"
    ];

    // Accepted values for the nbc column
    private static readonly List<string> NbcValues = ["NBC", "TRUE", "FALSE"];

    // Speedtest Template Column Positions (1-indexed) - dynamically computed
    private static class SpeedtestTemplateColumns
    {
        public static int Timestamp => Array.IndexOf(SpeedtestHeaders, "timestamp") + 1;
        public static int Upload => Array.IndexOf(SpeedtestHeaders, "upload") + 1;
        public static int Download => Array.IndexOf(SpeedtestHeaders, "download") + 1;
        public static int SignalQuality => Array.IndexOf(SpeedtestHeaders, "signal_quality") + 1;
        public static int Operator => Array.IndexOf(SpeedtestHeaders, "operator") + 1;
        public static int Latitude => Array.IndexOf(SpeedtestHeaders, "lat") + 1;
        public static int Longitude => Array.IndexOf(SpeedtestHeaders, "lon") + 1;
        public static int NetworkType => Array.IndexOf(SpeedtestHeaders, "network_type") + 1;
        public static int Nbc => Array.IndexOf(SpeedtestHeaders, "nbc") + 1;
    }

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
                await GenerateTemplate(package, worksheet => GenerateNAPTemplate(package, worksheet, cancellationToken));
                break;

            case FileTypeEnum.Speedtest:
                await GenerateTemplate(package, worksheet => GenerateSpeedtestTemplate(package, worksheet, cancellationToken));
                break;

            case FileTypeEnum.Cell:
                await GenerateTemplate(package, worksheet => GenerateCellTemplateVersion3(package, worksheet, cancellationToken));
                break;

            case FileTypeEnum.CellularCoverage:
                await GenerateTemplate(package, worksheet => GenerateCellularCoverageTemplate(package, worksheet, cancellationToken));
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

    private async Task GenerateTemplate(ExcelPackage package, Func<ExcelWorksheet, Task> TemplateGenerator)
    {
        var worksheet = package.Workbook.Worksheets.Add("Data");
        await TemplateGenerator(worksheet);
        AutoFitColumns(worksheet);
    }

    // Just duplicate and modify this method for more template
    private async Task GenerateNAPTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddCommonHeaders(worksheet, "Provider", "Barangay", "Municipality", "Province", "Region", "Lat", "Lng");

        var providers = await GetServiceProviders(ServiceTypeEnum.Wired, cancellationToken);

        if (providers.Count == 0)
        {
            throw new InvalidOperationException("No wired service providers available. Please add providers before generating the template.");
        }

        // Add providers to a hidden sheet
        var providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        var providerRange = providerSheet.Cells[1, 1, providers.Count, 1];
        providerSheet.Hidden = eWorkSheetHidden.Hidden;

        AddDropdownValidation(worksheet, "A2:A1048576", providerRange);
    }

    private async Task GenerateCellTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        // Define the multi-row header data
        object[,] headers = new object[,]
        {
            { "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "2G", "2G", "3G", "3G", "3G", "4G", "4G", "4G","4G", "4G", "4G", "4G", "4G", "5G", "5G", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (\u00B0)", "Beam Tilt (\u00B0)"},
            { "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "GSM", "GSM", "WCDMA", "WCDMA", "WCDMA", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "TDD-LTE", "TDD-LTE", "TDD-LTE", "", "", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (\u00B0)", "Beam Tilt (\u00B0)"},
            { "Provider", "Zone", "Barangay", "Municipality", "Province", "Region", "Lat", "Long", "900", "1800", "850", "900", "2100", "700", "850", "900", "1800", "2100", "2300", "2500", "2600", "2600", "3500", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (\u00B0)", "Beam Tilt (\u00B0)" }
        };

        // Apply the multi-row headers
        AddCellMultiRowHeaders(worksheet, headers, 1); // Start writing from row 1

        var providers = await GetServiceProviders(ServiceTypeEnum.Wireless, cancellationToken);
        var towerTypes = Enum.GetNames(typeof(TowerTypeEnum)).ToList();
        var antennaTypes = Enum.GetNames(typeof(AntennaTypeEnum)).ToList();
        List<string> trueFalseValues = ["TRUE", "FALSE"];

        // Add hidden sheets for dropdowns
        ExcelWorksheet providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        providerSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "A4:A1048576", providerSheet.Cells[1, 1, providers.Count, 1]);

        ExcelWorksheet towerTypeSheet = package.Workbook.Worksheets.Add("TowerTypes");
        towerTypeSheet.Cells.LoadFromCollection(towerTypes.Select(t => new { Type = t }));
        towerTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "X4:X1048576", towerTypeSheet.Cells[1, 1, towerTypes.Count, 1]);

        ExcelWorksheet antennaTypeSheet = package.Workbook.Worksheets.Add("AntennaTypes");
        antennaTypeSheet.Cells.LoadFromCollection(antennaTypes.Select(a => new { Type = a }));
        antennaTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "Y4:Y1048576", antennaTypeSheet.Cells[1, 1, antennaTypes.Count, 1]);

        // Add "True/False" dropdowns directly
        AddDropdownValidation(worksheet, "I4:I1048576", trueFalseValues, allowBlank: true); // "2G GSM 900"
        AddDropdownValidation(worksheet, "J4:J1048576", trueFalseValues, allowBlank: true); // "2G GSM 1800"
        AddDropdownValidation(worksheet, "K4:K1048576", trueFalseValues, allowBlank: true); // "3G WCDMA 850"
        AddDropdownValidation(worksheet, "L4:L1048576", trueFalseValues, allowBlank: true); // "3G WCDMA 900"
        AddDropdownValidation(worksheet, "M4:M1048576", trueFalseValues, allowBlank: true); // "3G WCDMA 2100"
        AddDropdownValidation(worksheet, "N4:N1048576", trueFalseValues, allowBlank: true); // "4G FDD-LTE 700"
        AddDropdownValidation(worksheet, "O4:O1048576", trueFalseValues, allowBlank: true); // "4G FDD-LTE 850"
        AddDropdownValidation(worksheet, "P4:P1048576", trueFalseValues, allowBlank: true); // "4G FDD-LTE 900"
        AddDropdownValidation(worksheet, "Q4:Q1048576", trueFalseValues, allowBlank: true); // "4G FDD-LTE 1800"
        AddDropdownValidation(worksheet, "R4:R1048576", trueFalseValues, allowBlank: true); // "4G FDD-LTE 2100"
        AddDropdownValidation(worksheet, "S4:S1048576", trueFalseValues, allowBlank: true); // "4G TDD-LTE 2300"
        AddDropdownValidation(worksheet, "T4:T1048576", trueFalseValues, allowBlank: true); // "4G TDD-LTE 2500"
        AddDropdownValidation(worksheet, "U4:U1048576", trueFalseValues, allowBlank: true); // "4G TDD-LTE 2600"
        AddDropdownValidation(worksheet, "V4:V1048576", trueFalseValues, allowBlank: true); // "5G 2600"
        AddDropdownValidation(worksheet, "W4:W1048576", trueFalseValues, allowBlank: true); // "5G 3500"
    }

    private async Task GenerateCellTemplateVersion3(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        // Define the multi-row header data
        object[,] headers = new object[,]
        {
            { "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "2G", "2G", "3G", "3G", "3G", "4G", "4G", "4G", "4G", "4G", "4G", "4G", "4G", "5G", "5G", "5G", "5G", "5G", "5G", "5G", "5G", "5G", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (\u00B0)", "Beam Tilt (\u00B0)"},
            { "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "GSM", "GSM", "WCDMA", "WCDMA", "WCDMA", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "TDD-LTE", "TDD-LTE", "TDD-LTE", "NR", "NR", "NR", "NR", "NR", "NR", "NR", "NR", "NR", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (\u00B0)", "Beam Tilt (\u00B0)"},
            { "Provider", "Zone", "Barangay", "Municipality", "Province", "Region", "Lat", "Long", "B8-900", "B3-1800", "B5-850", "B8-900", "B1-2100", "B28-700", "B5-850", "B8-900", "B3-1800", "B1-2100", "B40-2300", "B41-2500", "B38-2600", "B28-700", "B5-850", "B8-900", "B3-1800", "B1-2100", "B40-2300", "B41-2500", "B38-2600", "B78-3500", "Tower Type", "AntennaType", "Antenna Height (m)", "Azimuth (\u00B0)", "Beam Tilt (\u00B0)" }
        };

        // Apply the multi-row headers
        AddCellMultiRowHeaders(worksheet, headers, 1); // Start writing from row 1

        var providers = await GetServiceProviders(ServiceTypeEnum.Wireless, cancellationToken);
        var towerTypes = Enum.GetNames(typeof(TowerTypeEnum)).ToList();
        var antennaTypes = Enum.GetNames(typeof(AntennaTypeEnum)).ToList();
        List<string> trueFalseValues = ["TRUE", "FALSE"];

        if (providers.Count == 0)
        {
            throw new InvalidOperationException("No wireless service providers available. Please add providers before generating the template.");
        }

        // Add hidden sheets for dropdowns
        ExcelWorksheet providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        providerSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "A4:A1048576", providerSheet.Cells[1, 1, providers.Count, 1]);

        ExcelWorksheet towerTypeSheet = package.Workbook.Worksheets.Add("TowerTypes");
        towerTypeSheet.Cells.LoadFromCollection(towerTypes.Select(t => new { Type = t }));
        towerTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "AE4:AE1048576", towerTypeSheet.Cells[1, 1, towerTypes.Count, 1]);

        ExcelWorksheet antennaTypeSheet = package.Workbook.Worksheets.Add("AntennaTypes");
        antennaTypeSheet.Cells.LoadFromCollection(antennaTypes.Select(a => new { Type = a }));
        antennaTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "AF4:AF1048576", antennaTypeSheet.Cells[1, 1, antennaTypes.Count, 1]);

        // Add "TRUE/FALSE" dropdowns and conditional formatting for all 22 band columns (Cols 9 to 30: I through AD)
        for (int col = 9; col <= 30; col++)
        {
            var colName = GetExcelColumnName(col);
            AddDropdownValidation(worksheet, $"{colName}4:{colName}1048576", trueFalseValues, allowBlank: true);
        }
    }

    private async Task GenerateSpeedtestTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddCommonHeaders(worksheet, SpeedtestHeaders);

        var providers = await GetAllServiceProvidersAcrossTypes(cancellationToken);
        var technologies = await GetTechnologies(cancellationToken);
        var signalQuality = Enum.GetNames(typeof(SignalQualityEnum)).ToList();

        if (providers.Count == 0)
        {
            throw new InvalidOperationException("No service providers available. Please add providers before generating the template.");
        }

        if (technologies.Count == 0)
        {
            throw new InvalidOperationException("No technologies available. Please add technologies before generating the template.");
        }

        // Add providers to a hidden sheet
        var providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        var providerRange = providerSheet.Cells[1, 1, providers.Count, 1];
        providerSheet.Hidden = eWorkSheetHidden.Hidden;

        // Add technologies to a hidden sheet
        var technologySheet = package.Workbook.Worksheets.Add("Technologies");
        technologySheet.Cells.LoadFromCollection(technologies.Select(t => new { Name = t }));
        var technologyRange = technologySheet.Cells[1, 1, technologies.Count, 1];
        technologySheet.Hidden = eWorkSheetHidden.Hidden;

        // Apply dropdown validations using dynamic column positions
        var operatorCol = GetExcelColumnName(SpeedtestTemplateColumns.Operator);
        var networkTypeCol = GetExcelColumnName(SpeedtestTemplateColumns.NetworkType);
        var signalQualityCol = GetExcelColumnName(SpeedtestTemplateColumns.SignalQuality);

        AddDropdownValidation(worksheet, $"{operatorCol}2:{operatorCol}1048576", providerRange, allowBlank: false);
        AddDropdownValidation(worksheet, $"{networkTypeCol}2:{networkTypeCol}1048576", technologyRange, allowBlank: true);
        AddDropdownValidation(worksheet, $"{signalQualityCol}2:{signalQualityCol}1048576", signalQuality, allowBlank: true);

        // Add NBC column dropdown: blank = no broadband coverage false, "NBC" = true, "TRUE"/"FALSE" also accepted
        var nbcCol = GetExcelColumnName(SpeedtestTemplateColumns.Nbc);
        AddDropdownValidation(worksheet, $"{nbcCol}2:{nbcCol}1048576", NbcValues, allowBlank: true);

        // Mark required fields with red background color
        MarkRequiredFields(worksheet, new[]
        {
            SpeedtestTemplateColumns.Timestamp,
            SpeedtestTemplateColumns.Upload,
            SpeedtestTemplateColumns.Download,
            SpeedtestTemplateColumns.Operator,
            SpeedtestTemplateColumns.Latitude,
            SpeedtestTemplateColumns.Longitude
        });
    }

    private static void MarkRequiredFields(ExcelWorksheet worksheet, int[] requiredColumnNumbers)
    {
        var headerCells = requiredColumnNumbers.Select(colNum => worksheet.Cells[1, colNum]);
        
        foreach (var headerCell in headerCells)
        {
            headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
            headerCell.Style.Font.Color.SetColor(System.Drawing.Color.Black);
        }
    }

    private async Task GenerateCellularCoverageTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddCommonHeaders(worksheet, "ServiceProvider", "Network", "Lat", "Lng", "Strength", "Address");

        // Fetch data sequentially to avoid DbContext threading issues
        var serviceProviders = await GetWirelessServiceProviders(cancellationToken);
        var networks = await GetUniqueNetworksForDropdown(cancellationToken);

        // Add ServiceProvider dropdown using helper method
        AddDropdownWithHiddenSheetIfNeeded(
            package, worksheet, serviceProviders, 
            "ServiceProviders", "A2:A1048576");

        // Add Network dropdown using helper method
        AddDropdownWithHiddenSheetIfNeeded(
            package, worksheet, networks, 
            "Networks", "B2:B1048576");

        if (networks.Count == 0)
        {
            throw new InvalidOperationException("No networks available. Please add networks before generating the template.");
        }

        worksheet.Cells.AutoFitColumns();
    }

    /// <summary>
    /// Helper method to add dropdown validation with hidden sheet for large lists
    /// </summary>
    private void AddDropdownWithHiddenSheetIfNeeded(
        ExcelPackage package, 
        ExcelWorksheet worksheet, 
        List<string> values, 
        string hiddenSheetName, 
        string targetRange)
    {
        // Calculate if we need hidden sheet (200 char threshold or 50+ items)
        var totalLength = values.Sum(v => v.Length + 1);
        
        if (totalLength > 200 || values.Count > 50)
        {
            // Use hidden sheet for large lists
            var hiddenSheet = package.Workbook.Worksheets.Add(hiddenSheetName);
            hiddenSheet.Cells[1, 1].LoadFromCollection(values);
            hiddenSheet.Hidden = eWorkSheetHidden.VeryHidden;
            
            var range = hiddenSheet.Cells[1, 1, values.Count, 1];
            AddDropdownValidation(worksheet, targetRange, range);
        }
        else
        {
            // Use direct values for small lists
            AddDropdownValidation(worksheet, targetRange, values);
        }
    }

    private static void AddCommonHeaders(ExcelWorksheet worksheet, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }
        
        // Freeze the header row
        worksheet.View.FreezePanes(2, 1);
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
            (22, 30) // 5G
        };

        var technologyGroups = new List<(int Start, int End)>
        {
            (9, 10), // GSM
            (11, 13), // WCDMA
            (14, 18), // FDD-LTE
            (19, 21), // TDD-LTE
            (22, 30) // NR
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
        using (var range = worksheet.Cells[startRow, 31, startRow + 2, 31]) range.Merge = true; // Tower Type
        using (var range = worksheet.Cells[startRow, 32, startRow + 2, 32]) range.Merge = true; // Antenna Type
        using (var range = worksheet.Cells[startRow, 33, startRow + 2, 33]) range.Merge = true; // Antenna Height
        using (var range = worksheet.Cells[startRow, 34, startRow + 2, 34]) range.Merge = true; // Azimuth
        using (var range = worksheet.Cells[startRow, 35, startRow + 2, 35]) range.Merge = true; // Beam Tilt

        // Freeze the header rows
        worksheet.View.FreezePanes(startRow + numRows, 1);
    }

    private static void AddDropdownValidation(ExcelWorksheet worksheet, string range, ExcelRangeBase sourceRange, bool allowBlank = false, string? defaultValue = null)
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

        // Highlight pasted mismatches (turns cell light red with dark red text)
        var firstCell = range.Split(':')[0];
        var cf = worksheet.ConditionalFormatting.AddExpression(new ExcelAddress(range));
        cf.Formula = $"AND({firstCell}<>\"\", ISERROR(MATCH({firstCell}, {sourceRange.FullAddressAbsolute}, 0)))";
        cf.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cf.Style.Fill.BackgroundColor.Color = System.Drawing.Color.FromArgb(255, 199, 206);
        cf.Style.Font.Color.Color = System.Drawing.Color.FromArgb(156, 0, 6);

        if (!string.IsNullOrEmpty(defaultValue))
        {
            string cellAddress = range.Split(':')[0];
            ExcelRange targetCell = worksheet.Cells[cellAddress];
            targetCell.Value = defaultValue;
            targetCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
    }

    private static void AddDropdownValidation(ExcelWorksheet worksheet, string range, List<string> values, bool allowBlank = false, string? defaultValue = null)
    {
        // Use a hidden sheet for the values to enable both Excel Data Validation and Conditional Formatting
        var cleanRangeName = range.Replace(":", "_");
        var sheetName = "Lkp_" + cleanRangeName;
        if (sheetName.Length > 31) sheetName = sheetName[..31]; // Excel sheet names max 31 chars

        var existingSheet = worksheet.Workbook.Worksheets[sheetName];
        if (existingSheet == null)
        {
            existingSheet = worksheet.Workbook.Worksheets.Add(sheetName);
            existingSheet.Cells[1, 1].LoadFromCollection(values.Select(v => new { Value = v }));
            existingSheet.Hidden = eWorkSheetHidden.VeryHidden;
        }

        var sourceRange = existingSheet.Cells[1, 1, values.Count, 1];
        AddDropdownValidation(worksheet, range, sourceRange, allowBlank, defaultValue);
    }

    private static void AutoFitColumns(ExcelWorksheet worksheet)
    {
        for (int i = 1; i <= worksheet.Dimension.Columns; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    /// <summary>
    /// Converts a 1-indexed column number to Excel column letter notation (e.g., 1 -> "A", 27 -> "AA")
    /// </summary>
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

    private async Task<List<string>> GetServiceProviders(ServiceTypeEnum service, CancellationToken cancellationToken)
    {
        var query = _serviceProviderRepository.Query(sp => !sp.IsDeleted && sp.Service.Type == service)
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

    private async Task<List<string>> GetAllServiceProvidersAcrossTypes(CancellationToken cancellationToken)
    {
        var query = _serviceProviderRepository.Query(sp => !sp.IsDeleted)
            .OrderBy(sp => sp.Provider.Name)
            .Select(sp => sp.Provider.Name)
            .Distinct();

        return await _serviceProviderRepository.ToListAsync(query, cancellationToken);
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

    /// <summary>
    /// Gets unique network names ordered alphabetically for dropdown list
    /// </summary>
    private async Task<List<string>> GetUniqueNetworksForDropdown(CancellationToken cancellationToken)
    {
        var query = _networkRepository
            .Query(n => !n.IsDeleted)
            .Select(n => n.Name)
            .Distinct()
            .OrderBy(n => n);
        return await _networkRepository.ToListAsync(query, cancellationToken);
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
