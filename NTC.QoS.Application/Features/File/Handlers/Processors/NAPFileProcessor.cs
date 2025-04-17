using ClosedXML.Excel;
using FluentValidation;
using NTC.QoS.Application.Contracts;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
using System.Text.Json;

namespace NTC.QoS.Application.Features.File.Handlers.Processors;

public class NAPFileProcessor(INAPRepository nAPRepository, IServiceProviderRepository serviceProviderRepository, IZoneRepository zoneRepository) : IFileProcessor
{
    public FileTypeEnum FileType => FileTypeEnum.NAP;

    private readonly INAPRepository _napRecordRepository = nAPRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IZoneRepository _zoneRepository = zoneRepository;

    public async Task<ApiResponse<ProcessingResultDto>> ProcessFileAsync(Domain.Entities.File file)
    {
        var processingResults = new List<object>();
        bool hasErrors = false;
        int successfulCount = 0;
        int totalProcessed = 0;
        try
        {
            var jsonData = ParseExcelToJson(file.Content);
            var napRecords = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonData);

            if (napRecords is null || napRecords.Count is 0)
            {
                return new ApiResponse<ProcessingResultDto>
                {
                    Succeeded = true,
                    Message = "No NAP records found in the file.",
                    Data = new ProcessingResultDto { IsSuccess = true, Message = "No NAP records processed.", TotalProcessed = 0, SuccessfulCount = 0 }
                };
            }

            totalProcessed = napRecords.Count;

            foreach (var rowData in napRecords)
            {
                var recordResult = await ProcessRowDataAsync(rowData, file.Id);
                if (recordResult is NAPRowProcessingResult rowResult)
                {
                    if (!rowResult.IsSuccess)
                    {
                        processingResults.Add(rowResult);
                        hasErrors = true;
                    }
                    else
                    {
                        successfulCount++;
                    }
                }
                else
                {
                    hasErrors = true;
                    processingResults.Add(new { RowNumber = rowData.GetValueOrDefault("RowNumber"), ErrorMessage = "Unexpected processing result." });
                }
            }

            await _napRecordRepository.SaveChangesAsync();

            string overallMessage = hasErrors ? $"File processed with errors. {successfulCount} out of {totalProcessed} records processed successfully." : "File processed successfully.";
            return new ApiResponse<ProcessingResultDto>
            {
                Succeeded = !hasErrors,
                Message = overallMessage,
                Data = new ProcessingResultDto
                {
                    IsSuccess = !hasErrors,
                    Message = overallMessage,
                    Data = hasErrors ? processingResults : new List<object>(),
                    SuccessfulCount = successfulCount,
                    TotalProcessed = totalProcessed
                }
            };
        }
        catch (ValidationException ex)
        {
            return new ApiResponse<ProcessingResultDto>
            {
                Succeeded = false,
                Message = $"File validation failed: {ex.Message}",
                Data = new ProcessingResultDto { IsSuccess = false, Message = $"File validation failed: {ex.Message}", TotalProcessed = 0, SuccessfulCount = 0 }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing NAP file: {ex.Message}");
            return new ApiResponse<ProcessingResultDto>
            {
                Succeeded = false,
                Message = $"Error processing NAP file: {ex.Message}",
                Data = new ProcessingResultDto { IsSuccess = false, Message = $"Error processing NAP file: {ex.Message}", TotalProcessed = 0, SuccessfulCount = 0 }
            };
        }
    }

    private static string ParseExcelToJson(byte[] fileContent)
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
                rowData["RowNumber"] = row.RowNumber().ToString();
                jsonDataList.Add(rowData);
            }

            return JsonSerializer.Serialize(jsonDataList);
        }
        catch (ValidationException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Excel file: {ex.Message}");
            return "[]";
        }
    }

    private async Task<object> ProcessRowDataAsync(Dictionary<string, string> rowData, Guid fileId)
    {
        var rowNumber = rowData.TryGetValue("RowNumber", out var rn) && int.TryParse(rn, out var parsedRn) ? parsedRn : (int?)null;

        try
        {
            if (!rowData.TryGetValue("Service Provider", out var providerName))
            {
                return new NAPRowProcessingResult { IsSuccess = false, RowNumber = rowNumber, ErrorMessage = "Service Provider is required." };
            }

            rowData.TryGetValue("Municipality", out var rawMunicipalityName);
            string? municipalityName = rawMunicipalityName?.Replace(" City", "", StringComparison.OrdinalIgnoreCase).Trim();
            municipalityName = municipalityName?.Replace("City of ", "", StringComparison.OrdinalIgnoreCase).Trim();

            rowData.TryGetValue("Province", out var provinceName);
            rowData.TryGetValue("Region", out var regionName);
            if (!rowData.TryGetValue("Latitude", out var latitudeStr) || !decimal.TryParse(latitudeStr, out var latitude))
            {
                return new NAPRowProcessingResult { IsSuccess = false, RowNumber = rowNumber, ErrorMessage = "Latitude is required and must be a valid decimal." };
            }
            if (!rowData.TryGetValue("Longitude", out var longitudeStr) || !decimal.TryParse(longitudeStr, out var longitude))
            {
                return new NAPRowProcessingResult { IsSuccess = false, RowNumber = rowNumber, ErrorMessage = "Longitude is required and must be a valid decimal." };
            }

            var serviceProvider = await _serviceProviderRepository.FindByServiceAndProviderNamesAsync(ServiceTypeEnum.Wired.ToString(), providerName);
            if (serviceProvider is null)
            {
                return new NAPRowProcessingResult { IsSuccess = false, RowNumber = rowNumber, ErrorMessage = $"Service Provider '{providerName}' not found." };
            }

            var zone = await _zoneRepository.FindByNameAsync(municipalityName, provinceName, regionName);
            if (zone is null && (municipalityName != null || provinceName != null || regionName != null))
            {
                zone = await _zoneRepository.FindFirstAnyMatchAsync(municipalityName, provinceName, regionName);
                if (zone is null)
                {
                    return new NAPRowProcessingResult { IsSuccess = false, RowNumber = rowNumber, ErrorMessage = $"No Zone found matching the provided criteria (Municipality: '{municipalityName}', Province: '{provinceName}', Region: '{regionName}')." };
                }
            }
            else if (zone is null && municipalityName == null && provinceName == null && regionName == null)
            {
                return new NAPRowProcessingResult { IsSuccess = false, RowNumber = rowNumber, ErrorMessage = "All location identifiers (Municipality, Province, Region) are null." };
            }

            var napRecord = new Domain.Aggregates.NAP
            {
                ServiceProviderId = serviceProvider.Id,
                ZoneId = zone?.Id,
                CoorLatitude = latitude.ToString(),
                CoorLongitude = longitude.ToString(),
                FileId = fileId,
                RowNumber = rowNumber
            };
            _napRecordRepository.Add(napRecord);
            return new NAPRowProcessingResult { IsSuccess = true, Message = "Record processed successfully." };
        }
        catch (Exception ex)
        {
            return new NAPRowProcessingResult { IsSuccess = false, RowNumber = rowNumber, ErrorMessage = $"Error processing row: {ex.Message}" };
        }
    }
}

public class NAPRowProcessingResult
{
    public int? RowNumber { get; set; }
    public bool IsSuccess { get; set; } = false;
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
