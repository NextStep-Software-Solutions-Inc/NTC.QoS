using System.Text;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Represents a single validation or processing error encountered during file import.
/// </summary>
public class ImportErrorItem
{
    public int RowNumber { get; set; }
    public string Column { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuggestedResolution { get; set; } = string.Empty;
}

/// <summary>
/// Collects row-level import errors, creates self-service resolution summaries,
/// and generates downloadable CSV error reports stored in Blob Storage.
/// </summary>
public class ImportErrorLogger
{
    private readonly List<ImportErrorItem> _errors = new();
    private const int MaxTrackedErrors = 10000; // Cap to avoid runaway memory in giant files

    public bool HasErrors => _errors.Count > 0;
    public int ErrorCount => _errors.Count;
    public IReadOnlyList<ImportErrorItem> Errors => _errors;

    /// <summary>
    /// Records a row-level validation or processing error.
    /// </summary>
    public void AddError(int rowNumber, string column, string? value, string errorMessage, string suggestedResolution)
    {
        if (_errors.Count < MaxTrackedErrors)
        {
            _errors.Add(new ImportErrorItem
            {
                RowNumber = rowNumber,
                Column = column,
                Value = value,
                ErrorMessage = errorMessage,
                SuggestedResolution = suggestedResolution
            });
        }
    }

    /// <summary>
    /// Generates a concise user-friendly summary (up to 2000 characters) suitable for UI tooltips and file entity storage.
    /// </summary>
    public string GenerateSummary(int maxSampleErrors = 2)
    {
        if (_errors.Count == 0) return string.Empty;

        var sampleErrors = _errors.Take(maxSampleErrors).Select(e =>
            $"Row {e.RowNumber} [{e.Column}]: {e.ErrorMessage} (Value: '{e.Value ?? "<empty>"}')");

        var summary = $"Found {_errors.Count} invalid record(s). " + string.Join("; ", sampleErrors);
        var remaining = _errors.Count - maxSampleErrors;
        if (remaining > 0)
        {
            summary += $". (+{remaining} more. Download error report for full resolution steps.)";
        }
        else
        {
            summary += ". Download error report for full resolution steps.";
        }

        return summary.Length > 2000 ? summary[..1997] + "..." : summary;
    }

    /// <summary>
    /// Generates CSV report bytes formatted with RFC 4180 escaping and UTF-8 BOM for Microsoft Excel compatibility.
    /// </summary>
    public byte[] GenerateCsvBytes()
    {
        var sb = new StringBuilder();
        // UTF-8 BOM for Excel compatibility when opening CSV on Windows
        sb.Append('\uFEFF');
        sb.AppendLine("Row Number,Column,Invalid Value,Error Description,Suggested Resolution");

        foreach (var error in _errors)
        {
            sb.AppendLine(string.Join(",",
                error.RowNumber,
                EscapeCsv(error.Column),
                EscapeCsv(error.Value),
                EscapeCsv(error.ErrorMessage),
                EscapeCsv(error.SuggestedResolution)
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Uploads the CSV error report to Azure Blob Storage and returns the public/internal blob URL.
    /// </summary>
    public async Task<string?> UploadErrorLogAsync(
        Domain.Entities.File file,
        IBlobStorageService blobStorageService,
        string? environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!HasErrors) return null;

        try
        {
            var csvBytes = GenerateCsvBytes();
            using var stream = new MemoryStream(csvBytes);

            var containerName = GetContainerName(environment, file.Path);
            var safeFileName = Path.GetFileNameWithoutExtension(file.Name);
            var blobPath = $"error-logs/{file.Id:N}_{DateTime.UtcNow:yyyyMMddHHmmss}_{safeFileName}_errors.csv";

            logger.LogInformation("Uploading error report for file {FileId} to container {ContainerName}, blob {BlobPath}",
                file.Id, containerName, blobPath);

            var blobUrl = await blobStorageService.UploadAsync(
                containerName,
                blobPath,
                stream,
                "text/csv",
                cancellationToken);

            return blobUrl;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload error report for file {FileId}", file.Id);
            return null;
        }
    }

    private static string GetContainerName(string? environment, string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            try
            {
                var (containerName, _) = ProcessingJobHelper.ExtractBlobInfo(uri);
                if (!string.IsNullOrEmpty(containerName)) return containerName;
            }
            catch
            {
                // Fallback to environment check
            }
        }

        return environment?.ToLowerInvariant() switch
        {
            "production" or "staging" => "production",
            _ => "development"
        };
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return $"\"{field}\"";
    }
}
