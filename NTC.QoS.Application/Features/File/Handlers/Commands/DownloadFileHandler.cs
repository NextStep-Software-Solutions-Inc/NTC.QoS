using MediatR;
using NTC.QoS.Application.Features.File.Requests;
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
    private readonly IRepository<Domain.Entities.Zone> _zoneRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;
    private readonly IRepository<Domain.Aggregates.Generation> _generationRepository;

    public DownloadFileHandler(
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.Zone> zoneRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository,
        IRepository<Domain.Aggregates.Generation> generationRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _zoneRepository = zoneRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _generationRepository = generationRepository;
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
            // Add cases for other file types as needed
            // case FileTypeEnum.OtherType:
            //     await GenerateTemplate(package, cancellationToken, GenerateOtherTemplate);
            //     break;

            case FileTypeEnum.Cell:
                await GenerateTemplate(package, worksheet => GenerateCellTemplate(package, worksheet, cancellationToken), cancellationToken);
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
        AddHeaders(worksheet, "Provider", "Municipality", "Province", "Lat", "Lng");

        var providers = await GetServiceProviders(cancellationToken);
        var municipalities = await GetMunicipalities(cancellationToken);
        var provinces = await GetProvinces(cancellationToken);

        // Add providers to a hidden sheet
        var providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        var providerRange = providerSheet.Cells[1, 1, providers.Count, 1];
        providerSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "A2:A10", providerRange);

        // Add municipalities to a hidden sheet
        var municipalitySheet = package.Workbook.Worksheets.Add("Municipalities");
        municipalitySheet.Cells.LoadFromCollection(municipalities.Select(m => new { Name = m }));
        var municipalityRange = municipalitySheet.Cells[1, 1, municipalities.Count, 1];
        municipalitySheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "B2:B10", municipalityRange);

        // Add provinces to a hidden sheet
        var provinceSheet = package.Workbook.Worksheets.Add("Provinces");
        provinceSheet.Cells.LoadFromCollection(provinces.Select(p => new { Name = p }));
        var provinceRange = provinceSheet.Cells[1, 1, provinces.Count, 1];
        provinceSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "C2:C10", provinceRange);
    }

    private async Task GenerateCellTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        // Define the multi-row header data
        object[,] headers = new object[,]
        {
            { "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "Generation", "2G", "2G", "3G", "3G", "4G", "4G", "4G", "4G", "4G", "4G", "4G", "5G", "5G", "Antenna Height (m)", "Tower Type", "Azimuth (°)"},
            { "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "Technology", "GSM", "GSM", "WCDMA", "WCDMA", "FDD-LTE", "FDD-LTE", "FDD-LTE", "FDD-LTE", "TDD-LTE", "TDD-LTE", "TDD-LTE", "", "", "Antenna Height (m)", "Tower Type", "Azimuth (°)"},
            { "Provider", "Zone", "Barangay", "Municipality", "Province", "Region", "Lat", "Long", "900", "1800", "850", "2100", "2100", "900", "1800", "700", "2300", "2500", "2600", "2600", "3500", "Antenna Height (m)", "Tower Type", "Azimuth (°)" }
        };

        // Apply the multi-row headers
        AddMultiRowHeaders(worksheet, headers, 1); // Start writing from row 1

        var providers = await GetServiceProviders(cancellationToken);
        var zones = await GetZones(cancellationToken);
        var barangays = await GetBarangays(cancellationToken);
        var municipalities = await GetMunicipalities(cancellationToken);
        var provinces = await GetProvinces(cancellationToken);
        var regions = await GetRegions(cancellationToken);
        var towerTypes = Enum.GetNames(typeof(TowerTypeEnum)).ToList();
        string[] trueFalseValues = { "TRUE", "FALSE" };

        // Add hidden sheets for dropdowns
        ExcelWorksheet providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        providerSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "A4:A10", providerSheet.Cells[1, 1, providers.Count, 1]);

        ExcelWorksheet zoneSheet = package.Workbook.Worksheets.Add("Zones");
        zoneSheet.Cells.LoadFromCollection(zones.Select(z => new { Name = z }));
        zoneSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "B4:B10", zoneSheet.Cells[1, 1, zones.Count, 1]);

        ExcelWorksheet barangaySheet = package.Workbook.Worksheets.Add("Barangays");
        barangaySheet.Cells.LoadFromCollection(barangays.Select(b => new { Name = b }));
        barangaySheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "C4:C10", barangaySheet.Cells[1, 1, barangays.Count, 1]);

        ExcelWorksheet municipalitySheet = package.Workbook.Worksheets.Add("Municipalities");
        municipalitySheet.Cells.LoadFromCollection(municipalities.Select(m => new { Name = m }));
        municipalitySheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "D4:D10", municipalitySheet.Cells[1, 1, municipalities.Count, 1]);

        ExcelWorksheet provinceSheet = package.Workbook.Worksheets.Add("Provinces");
        provinceSheet.Cells.LoadFromCollection(provinces.Select(p => new { Name = p }));
        provinceSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "E4:E10", provinceSheet.Cells[1, 1, provinces.Count, 1]);

        ExcelWorksheet regionSheet = package.Workbook.Worksheets.Add("Regions");
        regionSheet.Cells.LoadFromCollection(regions.Select(r => new { Name = r }));
        regionSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "F4:F10", regionSheet.Cells[1, 1, regions.Count, 1]);

        ExcelWorksheet towerTypeSheet = package.Workbook.Worksheets.Add("TowerTypes");
        towerTypeSheet.Cells.LoadFromCollection(towerTypes.Select(t => new { Type = t }));
        towerTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "W4:W10", towerTypeSheet.Cells[1, 1, towerTypes.Count, 1]);

        // Add "True/False" dropdowns directly
        AddDropdownValidation(worksheet, "I4:I10", trueFalseValues.ToList()); // "2G GSM 900"
        AddDropdownValidation(worksheet, "J4:J10", trueFalseValues.ToList()); // "2G GSM 1800"
        AddDropdownValidation(worksheet, "K4:K10", trueFalseValues.ToList()); // "3G WCDMA 850"
        AddDropdownValidation(worksheet, "L4:L10", trueFalseValues.ToList()); // "3G WCDMA 2100"
        AddDropdownValidation(worksheet, "M4:M10", trueFalseValues.ToList()); // "4G FDD-LTE 2100"
        AddDropdownValidation(worksheet, "N4:N10", trueFalseValues.ToList()); // "4G FDD-LTE 900"
        AddDropdownValidation(worksheet, "O4:O10", trueFalseValues.ToList()); // "4G FDD-LTE 1800"
        AddDropdownValidation(worksheet, "P4:P10", trueFalseValues.ToList()); // "4G FDD-LTE 700"
        AddDropdownValidation(worksheet, "Q4:Q10", trueFalseValues.ToList()); // "4G TDD-LTE 2300"
        AddDropdownValidation(worksheet, "R4:R10", trueFalseValues.ToList()); // "4G TDD-LTE 2500"
        AddDropdownValidation(worksheet, "S4:S10", trueFalseValues.ToList()); // "4G TDD-LTE 2600"
        AddDropdownValidation(worksheet, "T4:T10", trueFalseValues.ToList()); // "5G 2600"
        AddDropdownValidation(worksheet, "U4:U10", trueFalseValues.ToList()); // "5G 3500"
    }

    private async Task GenerateSpeedtestTemplate(ExcelPackage package, ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddHeaders(worksheet, "Region", "Province", "City", "Barangay", "Timestamp", "Generation", "Provider", "Upload", "Download", "RSSI", "Remarks");

        var regions = await GetRegions(cancellationToken);
        var provinces = await GetProvinces(cancellationToken);
        var cities = await GetMunicipalities(cancellationToken);
        var barangays = await GetBarangays(cancellationToken);
        var providers = await GetServiceProviders(cancellationToken);
        var generations = await GetGenerations(cancellationToken);
        var remarks = Enum.GetNames(typeof(SignalQualityEnum)).ToList();

        AddDropdownValidation(worksheet, "A2:A10", regions);
        AddDropdownValidation(worksheet, "B2:B10", provinces);
        AddDropdownValidation(worksheet, "C2:C10", cities);
        AddDropdownValidation(worksheet, "D2:D10", barangays);
        AddDropdownValidation(worksheet, "F2:F10", generations);
        AddDropdownValidation(worksheet, "G2:G10", providers);
        AddDropdownValidation(worksheet, "K2:K10", remarks);
    }

    private static void AddHeaders(ExcelWorksheet worksheet, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }
    }

    private static void AddMultiRowHeaders(ExcelWorksheet worksheet, object[,] headers, int startRow)
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
            (11, 12), // 3G
            (13, 19), // 4G
            (20, 21) // 5G
        };

        var technologyGroups = new List<(int Start, int End)>
        {
            (9, 10), // GSM
            (11, 12), // WCDMA
            (13, 16), // FDD-LTE
            (17, 19), // TDD-LTE
            (20, 21) // 5G
        };

        // Apply merging for "Generation"
        foreach (var group in generationGroups)
        {
            using (var range = worksheet.Cells[startRow, group.Start, startRow, group.End])
            {
                range.Merge = true;
            }
        }

        // Apply merging for "Technology"
        foreach (var group in technologyGroups)
        {
            using (var range = worksheet.Cells[startRow + 1, group.Start, startRow + 1, group.End])
            {
                range.Merge = true;
            }
        }

        // Merge the last three columns vertically
        using (var range = worksheet.Cells[startRow, 22, startRow + 2, 22]) range.Merge = true; // Antenna Height
        using (var range = worksheet.Cells[startRow, 23, startRow + 2, 23]) range.Merge = true; // Tower Type
        using (var range = worksheet.Cells[startRow, 24, startRow + 2, 24]) range.Merge = true; // Azimuth

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

    private async Task<List<string>> GetZones(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_zoneRepository, z => !z.IsDeleted, z => z.Name, cancellationToken);
    }
    private async Task<List<string>> GetBarangays(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_barangayRepository, b => !b.IsDeleted, b => b.Name, cancellationToken);
    }

    private async Task<List<string>> GetMunicipalities(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_municipalityRepository, m => !m.IsDeleted, m => m.Name, cancellationToken);
    }

    private async Task<List<string>> GetProvinces(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_provinceRepository, p => !p.IsDeleted, p => p.Name, cancellationToken);
    }

    private async Task<List<string>> GetRegions(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_regionRepository, r => !r.IsDeleted, r => r.Name, cancellationToken);
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
        return await GetEntityNames(_generationRepository, g => !g.IsDeleted, g=> g.Name, cancellationToken);
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
