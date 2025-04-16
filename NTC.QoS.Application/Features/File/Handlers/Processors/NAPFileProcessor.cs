using ClosedXML.Excel;
using FluentValidation;
using NTC.QoS.Application.Contracts;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.Text.Json;

namespace NTC.QoS.Application.Features.File.Handlers.Processors;

public class NAPFileProcessor (INAPRepository nAPRepository, IServiceProviderRepository serviceProviderRepository, IZoneRepository zoneRepository): IFileProcessor
{
    public FileTypeEnum FileType => FileTypeEnum.NAP;

    public readonly INAPRepository _napRecordRepository = nAPRepository;
    public readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    public readonly IZoneRepository _zoneRepository = zoneRepository;

    public async Task ProcessFileAsync(Domain.Entities.File file)
    {
        try
        {
            var jsonData = ParseExcelToJson(file.Content);
            await ProcessJsonDataAsync(jsonData, file.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing NAP file: {ex.Message}");
            throw;
        }
    }

    private string ParseExcelToJson(byte[] fileContent)
    {
        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(fileContent));
            var worksheet = workbook.Worksheets.First();
            var jsonDataList = new List<Dictionary<string, string>>();

            var headerRow = worksheet.FirstRowUsed() ?? throw new ValidationException("No header row found in the Excel file.");
            var expectedHeaders = new[] { "Service Provider", "Municipality", "Province", "Region", "Latitude", "Longitude" };
            var actualHeaders = headerRow.CellsUsed().Select(c => c.Value.ToString().Trim()).ToArray();

            if (!expectedHeaders.SequenceEqual(actualHeaders))
            {
                throw new ValidationException($"Invalid NAP file headers. Expected: '{string.Join("\t", expectedHeaders)}', Actual: '{string.Join("\t", actualHeaders)}'");
            }

            int headerColumnCount = headerRow.CellsUsed().Count();
            var dataRows = worksheet.RowsUsed().Skip(1);

            foreach (var row in dataRows)
            {
                var rowData = new Dictionary<string, string>();
                for (int i = 1; i <= headerColumnCount; i++)
                {
                    var header = headerRow.Cell(i).Value.ToString();
                    var value = row.Cell(i).Value.ToString();
                    if (!string.IsNullOrEmpty(header))
                    {
                        rowData[header] = value;
                    }
                }
                jsonDataList.Add(rowData);
            }

            return JsonSerializer.Serialize(jsonDataList);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Excel file: {ex.Message}");
            return "[]";
        }
    }

    private async Task ProcessJsonDataAsync(string jsonData, Guid fileId)
    {
        var napRecords = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonData);

        if (napRecords is null || napRecords.Count is 0)
        {
            Console.WriteLine("No NAP records found in the file.");
            return;
        }

        foreach (var rowData in napRecords)
        {
            try
            {
                if (!rowData.TryGetValue("Service Provider", out var providerName)) continue;
                rowData.TryGetValue("Municipality", out var rawMunicipalityName);
                string? municipalityName = rawMunicipalityName?.Replace(" City", "", StringComparison.OrdinalIgnoreCase).Trim();
                rowData.TryGetValue("Province", out var provinceName);
                rowData.TryGetValue("Region", out var regionName);
                if (!rowData.TryGetValue("Latitude", out var latitude)) continue;
                if (!rowData.TryGetValue("Longitude", out var longitude)) continue;

                // Extract RowNumber
                rowData.TryGetValue("RowNumber", out var rowNumberObj);
                int? rowNumber = null;
                if (rowNumberObj != null && int.TryParse(rowNumberObj.ToString(), out var parsedRowNumber))
                {
                    rowNumber = parsedRowNumber;
                }

                // Lookup Service Provider from DBs
                var serviceProvider = await _serviceProviderRepository.FindByServiceAndProviderNamesAsync(ServiceTypeEnum.Wired.ToString(), providerName);
                if (serviceProvider is null)
                {
                    Console.WriteLine($"Service Provider '{providerName}' not found. Skipping record.");
                    continue;
                }

                // Lookup Zone from DB, allowing nulls and finding any match
                var zone = await _zoneRepository.FindByNameAsync(municipalityName, provinceName, regionName);
                if (zone is null && (municipalityName != null || provinceName != null || regionName != null))
                {
                    // If the initial lookup failed with some non-null filters, try to find *any* match
                    zone = await _zoneRepository.FindFirstAnyMatchAsync(municipalityName, provinceName, regionName);
                    if (zone is null)
                    {
                        Console.WriteLine($"No Zone found matching the provided criteria (Municipality: '{municipalityName}', Province: '{provinceName}', Region: '{regionName}'). Skipping record.");
                        continue;
                    }
                }
                else if (zone is null && municipalityName == null && provinceName == null && regionName == null)
                {
                    Console.WriteLine($"All location identifiers (Municipality, Province, Region) are null. Skipping record.");
                    continue;
                }

                var napRecord = new Domain.Aggregates.NAP
                {
                    ServiceProviderId = serviceProvider.Id,
                    ZoneId = zone?.Id,
                    CoorLatitude = latitude,
                    CoorLongitude = longitude,
                    FileId = fileId,
                    RowNumber = rowNumber
                };
                _napRecordRepository.Add(napRecord);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing a row: {ex.Message}");
                // Decide if you want to continue processing other rows or stop
            }
        }

        await _napRecordRepository.SaveChangesAsync();
    }
}
