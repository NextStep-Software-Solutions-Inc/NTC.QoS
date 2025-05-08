using MediatR;
using NTC.QoS.Application.Features.Barangay.Handlers.Queries;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

internal sealed class DownloadFileHandler : IRequestHandler<DownloadTemplateFileRequest, ApiResponse<Stream>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _serviceProviderRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository;
    private readonly IRepository<Domain.Aggregates.Generation> _generationRepository;
    private readonly IRepository<Domain.Entities.Technology> _technologyRepository;
    private readonly IRepository<Domain.Aggregates.Cell> _cellRepository;


    public DownloadFileHandler(
        IRepository<Domain.Aggregates.ServiceProvider> serviceProviderRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Aggregates.Generation> generationRepository,
        IRepository<Domain.Entities.Technology> technologyRepository,
        IRepository<Domain.Aggregates.Cell> cellRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _barangayRepository = barangayRepository;
        _generationRepository = generationRepository;
        _technologyRepository = technologyRepository;
        _cellRepository = cellRepository;
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
                await GenerateTemplate(package, cancellationToken, GenerateNAPTemplate);
                break;

            case FileTypeEnum.Speedtest:
                await GenerateTemplate(package, cancellationToken, GenerateSpeedtestTemplate);
                break;
            // Add cases for other file types as needed
            // case FileTypeEnum.OtherType:
            //     await GenerateTemplate(package, cancellationToken, GenerateOtherTemplate);
            //     break;

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

    private async Task GenerateTemplate(ExcelPackage package, CancellationToken cancellationToken, Func<ExcelWorksheet, CancellationToken, Task> TemplateGenerator)
    {
        var worksheet = package.Workbook.Worksheets.Add("Data");
        await TemplateGenerator(worksheet, cancellationToken);
        AutoFitColumns(worksheet);
    }
    // Just duplicate and modify this method for more template
    private async Task GenerateNAPTemplate(ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddHeaders(worksheet, "Provider", "Municipality", "Province", "Lat", "Lng");

        var providers = await GetServiceProviders(cancellationToken);
        var municipalities = await GetMunicipalities(cancellationToken);
        var provinces = await GetProvinces(cancellationToken);

        AddDropdownValidation(worksheet, "A2:A10", providers);
        AddDropdownValidation(worksheet, "B2:B10", municipalities);
        AddDropdownValidation(worksheet, "C2:C10", provinces);
    }

    private async Task GenerateSpeedtestTemplate(ExcelWorksheet worksheet, CancellationToken cancellationToken)
    {
        AddHeaders(worksheet, "timestamp", "ping", "jitter", "upload", "download", "region", "province", "municipality", "barangay", "rssi", "signal_quality", "operator", "lat", "lon", "phone_model", "email", "nro", "network_type", "cell_id", "MCC", "MNC", "tac");

        var regions = await GetRegions(cancellationToken);
        var provinces = await GetProvinces(cancellationToken);
        var cities = await GetMunicipalities(cancellationToken);
        var barangays = await GetBarangays(cancellationToken);
        var providers = await GetWirelessServiceProviders(cancellationToken);
        var technologies = await GetTechnologies(cancellationToken);
        var signalQuality = Enum.GetNames(typeof(SignalQualityEnum)).ToList();
        var CID = await GetCIDs(cancellationToken);

        AddDropdownValidation(worksheet, "F2:F10", regions);
        AddDropdownValidation(worksheet, "G2:G10", provinces);
        AddDropdownValidation(worksheet, "H2:H10", cities);
        AddDropdownValidation(worksheet, "I2:I10", barangays);
        AddDropdownValidation(worksheet, "L2:L10", providers);
        AddDropdownValidation(worksheet, "R2:R10", technologies);
        AddDropdownValidation(worksheet, "K2:K10", signalQuality);
        AddDropdownValidation(worksheet, "S2:S10", CID);
    }

    private static void AddHeaders(ExcelWorksheet worksheet, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }
    }

    private static void AddDropdownValidation(ExcelWorksheet worksheet, string range, List<string> values)
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
    }

    private static void AutoFitColumns(ExcelWorksheet worksheet)
    {
        for (int i = 1; i <= worksheet.Dimension.Columns; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private async Task<List<string>> GetMunicipalities(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_municipalityRepository, m => !m.IsDeleted, m => m.Name, cancellationToken);
    }

    private async Task<List<string>> GetProvinces(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_provinceRepository, p => !p.IsDeleted, p => p.Name, cancellationToken);
    }

    private async Task<List<string>> GetServiceProviders(CancellationToken cancellationToken)
    {
        var query = _serviceProviderRepository.Query(sp => !sp.IsDeleted && sp.Service.Type == ServiceTypeEnum.Wired)
            .OrderBy(sp => sp.Provider.Name)
            .Select(sp => sp.Provider.Name);

        return await _serviceProviderRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<string>> GetRegions(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_regionRepository, r => !r.IsDeleted, r => r.Name, cancellationToken);
    }
    
    private async Task<List<string>> GetBarangays(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_barangayRepository, b => !b.IsDeleted, b => b.Name, cancellationToken);
    }

    private async Task<List<string>> GetGenerations(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_generationRepository, g => !g.IsDeleted, g => g.Name, cancellationToken);
    }

    private async Task<List<string>> GetTechnologies(CancellationToken cancellationToken)
    {
        return await GetEntityNames(_technologyRepository, t => !t.IsDeleted, t => t.Standard, cancellationToken);
    }

    private async Task<List<string>> GetCIDs(CancellationToken cancellationToken)
    {
        var query = _cellRepository.Query(c => !c.IsDeleted)
            .OrderBy(c => c.CID)
            .Select(c => c.CID);

        return await _cellRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<string>> GetWirelessServiceProviders(CancellationToken cancellationToken)
    {
        var query = _serviceProviderRepository.Query(sp => !sp.IsDeleted && sp.Service.Type == ServiceTypeEnum.Wireless)
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
