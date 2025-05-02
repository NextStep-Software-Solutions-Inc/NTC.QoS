using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.FormulaParsing.Excel;
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

    public DownloadFileHandler(
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.Zone> zoneRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _zoneRepository = zoneRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
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
        AddHeaders(worksheet, "Provider", "Zone", "Barangay", "Municipality", "Province", "Region", "Lat", "Long",
                            "2G GSM 900", "2G GSM 1800", "3G WCDMA 850", "3G WCDMA 2100", "4G FDD-LTE 2100", "4G FDD-LTE 900",
                            "4G FDD-LTE 1800", "4G FDD-LTE 700", "4G TDD-LTE 2300", "4G TDD-LTE 2500", "4G TDD-LTE 2600",
                            "5G 2600", "5G 3500", "Antenna Height (m)", "Tower Type", "Azimuth (°)");

        var providers = await GetServiceProviders(cancellationToken);
        var zones = await GetZones(cancellationToken);
        var barangays = await GetBarangays(cancellationToken);
        var municipalities = await GetMunicipalities(cancellationToken);
        var provinces = await GetProvinces(cancellationToken);
        var regions = await GetRegions(cancellationToken);
        var towerTypes = Enum.GetNames(typeof(TowerTypeEnum)).ToList();
        string[] trueFalseValues = { "TRUE", "FALSE" };

        // Add providers to a hidden sheet
        ExcelWorksheet providerSheet = package.Workbook.Worksheets.Add("Providers");
        providerSheet.Cells.LoadFromCollection(providers.Select(p => new { Name = p }));
        ExcelRange providerRange = providerSheet.Cells[1, 1, providers.Count, 1];
        providerSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "A2:A10", providerRange);

        // Add zones to a hidden sheet
        var zoneSheet = package.Workbook.Worksheets.Add("Zones");
        zoneSheet.Cells.LoadFromCollection(zones.Select(z => new { Name = z }));
        var zoneRange = zoneSheet.Cells[1, 1, zones.Count, 1];
        zoneSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "B2:B10", zoneRange);

        // Add barangays to a hidden sheet
        var barangaySheet = package.Workbook.Worksheets.Add("Barangays");
        barangaySheet.Cells.LoadFromCollection(barangays.Select(b => new { Name = b }));
        var barangayRange = barangaySheet.Cells[1, 1, barangays.Count, 1];
        barangaySheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "C2:C10", barangayRange);

        // Add municipalities to a hidden sheet
        var municipalitySheet = package.Workbook.Worksheets.Add("Municipalities");
        municipalitySheet.Cells.LoadFromCollection(municipalities.Select(m => new { Name = m }));
        var municipalityRange = municipalitySheet.Cells[1, 1, municipalities.Count, 1];
        municipalitySheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "D2:D10", municipalityRange);

        // Add provinces to a hidden sheet
        var provinceSheet = package.Workbook.Worksheets.Add("Provinces");
        provinceSheet.Cells.LoadFromCollection(provinces.Select(p => new { Name = p }));
        var provinceRange = provinceSheet.Cells[1, 1, provinces.Count, 1];
        provinceSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "E2:E10", provinceRange);

        // Add regions to a hidden sheet
        var regionSheet = package.Workbook.Worksheets.Add("Regions");
        regionSheet.Cells.LoadFromCollection(regions.Select(r => new { Name = r }));
        var regionRange = regionSheet.Cells[1, 1, regions.Count, 1];
        regionSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "F2:F10", regionRange);

        // Add tower types to a hidden sheet
        var towerTypeSheet = package.Workbook.Worksheets.Add("TowerTypes");
        towerTypeSheet.Cells.LoadFromCollection(towerTypes.Select(t => new { Type = t }));
        var towerTypeRange = towerTypeSheet.Cells[1, 1, towerTypes.Count, 1];
        towerTypeSheet.Hidden = eWorkSheetHidden.Hidden;
        AddDropdownValidation(worksheet, "W2:W10", towerTypeRange);

        // Add "True/False" dropdowns directly
        AddDropdownValidation(worksheet, "I2:I10", trueFalseValues.ToList(), "FALSE"); // "2G GSM 900"
        AddDropdownValidation(worksheet, "J2:J10", trueFalseValues.ToList(), "FALSE"); // "2G GSM 1800"
        AddDropdownValidation(worksheet, "K2:K10", trueFalseValues.ToList(), "FALSE"); // "3G WCDMA 850"
        AddDropdownValidation(worksheet, "L2:L10", trueFalseValues.ToList(), "FALSE"); // "3G WCDMA 2100"
        AddDropdownValidation(worksheet, "M2:M10", trueFalseValues.ToList(), "FALSE"); // "4G FDD-LTE 2100"
        AddDropdownValidation(worksheet, "N2:N10", trueFalseValues.ToList(), "FALSE"); // "4G FDD-LTE 900"
        AddDropdownValidation(worksheet, "O2:O10", trueFalseValues.ToList(), "FALSE"); // "4G FDD-LTE 1800"
        AddDropdownValidation(worksheet, "P2:P10", trueFalseValues.ToList(), "FALSE"); // "4G FDD-LTE 700"
        AddDropdownValidation(worksheet, "Q2:Q10", trueFalseValues.ToList(), "FALSE"); // "4G TDD-LTE 2300"
        AddDropdownValidation(worksheet, "R2:R10", trueFalseValues.ToList(), "FALSE"); // "4G TDD-LTE 2500"
        AddDropdownValidation(worksheet, "S2:S10", trueFalseValues.ToList(), "FALSE"); // "4G TDD-LTE 2600"
        AddDropdownValidation(worksheet, "T2:T10", trueFalseValues.ToList(), "FALSE"); // "5G 2600"
        AddDropdownValidation(worksheet, "U2:U10", trueFalseValues.ToList(), "FALSE"); // "5G 3500"
    }

    private void AddHeaders(ExcelWorksheet worksheet, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }
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
            .Select(sp => sp.Provider.Name);

        return await _serviceProviderRepository.ToListAsync(query, cancellationToken);
    }

    private static async Task<List<string>> GetEntityNames<TEntity>(
        IRepository<TEntity> repository,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, string>> selector,
        CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var query = repository.Query(predicate).OrderBy(selector).Select(selector);
        return await repository.ToListAsync(query, cancellationToken);
    }
}
