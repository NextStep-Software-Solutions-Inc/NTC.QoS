using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Models.CsvBindings;
using System.Globalization;

namespace NTC.QoS.Infrastructure.FileProcessing;

/// <summary>
/// CSV file parsing utility for CellularCoverage V2
/// </summary>
public static class CsvParser
{
    /// <summary>
    /// Streams CSV records in batches to avoid loading entire file into memory.
    /// Perfect for large files (100MB+).
    /// </summary>
    /// <param name="stream">CSV file stream</param>
    /// <param name="batchSize">Number of records to yield per batch</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>Enumerable of batched records</returns>
    public static IEnumerable<List<CellularCoverageV2CsvRecord>> StreamCellularCoverageV2CsvBatches(
        Stream stream,
        int batchSize = 1000,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            Delimiter = ";",
            MissingFieldFound = null,
            BadDataFound = context =>
            {
                logger?.LogWarning(
                    "Bad data found at row {Row}, field {Field}: {RawRecord}",
                    context.Context?.Parser?.Row ?? 0,
                    context.Field ?? string.Empty,
                    context.RawRecord ?? string.Empty);
            },
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        };

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<CellularCoverageV2CsvMap>();

        logger?.LogInformation("Starting streaming CSV parse with batch size {BatchSize}", batchSize);

        var batch = new List<CellularCoverageV2CsvRecord>(batchSize);
        var records = csv.GetRecords<CellularCoverageV2CsvRecord>();

        foreach (var record in records)
        {
            batch.Add(record);

            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<CellularCoverageV2CsvRecord>(batchSize);
            }
        }

        // Yield remaining records
        if (batch.Count > 0)
        {
            yield return batch;
        }

        logger?.LogInformation("Completed streaming CSV parse");
    }

    /// <summary>
    /// Parses CSV stream into CellularCoverageV2CsvRecord objects (legacy method - loads all into memory)
    /// Consider using StreamCellularCoverageV2CsvBatches for large files
    /// </summary>
    /// <param name="stream">CSV file stream</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>List of parsed records</returns>
    /// <exception cref="ArgumentNullException">If stream is null</exception>
    /// <exception cref="CsvHelper.CsvHelperException">If CSV format is invalid</exception>
    [Obsolete("Use StreamCellularCoverageV2CsvBatches for better memory efficiency with large files")]
    public static List<CellularCoverageV2CsvRecord> ParseCellularCoverageV2Csv(
        Stream stream,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var records = new List<CellularCoverageV2CsvRecord>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false, // Files have no headers, use index-based mapping
            Delimiter = ";", // CSV uses semicolon delimiter
            MissingFieldFound = null,
            BadDataFound = context =>
            {
                logger?.LogWarning(
                    "Bad data found at row {Row}, field {Field}: {RawRecord}",
                    context.Context?.Parser?.Row ?? 0,
                    context.Field ?? string.Empty,
                    context.RawRecord ?? string.Empty);
            },
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        };

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        // Register class map for column mapping
        csv.Context.RegisterClassMap<CellularCoverageV2CsvMap>();

        try
        {
            logger?.LogInformation("Starting CSV parsing for CellularCoverage V2");

            // Read all records
            records = csv.GetRecords<CellularCoverageV2CsvRecord>().ToList();

            logger?.LogInformation("Successfully parsed {Count} records from CSV", records.Count);
        }
        catch (CsvHelper.HeaderValidationException ex)
        {
            logger?.LogError(ex, "CSV header validation failed. Expected columns: Lat, Lng, Signal, Address");
            throw new InvalidOperationException(
                "CSV file must contain the following columns: Lat, Lng, Signal, Address", ex);
        }
        catch (CsvHelper.CsvHelperException ex)
        {
            logger?.LogError(ex, "CSV parsing failed at row {Row}", csv.Context?.Parser?.Row ?? 0);
            throw;
        }

        return records;
    }

    /// <summary>
    /// Validates that a CSV stream has the required headers
    /// </summary>
    public static bool ValidateCsvHeaders(Stream stream, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var originalPosition = stream.Position;
        stream.Position = 0;

        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            var requiredHeaders = new[] { "Lat", "Lng", "Signal", "Address" };
            var headerRecord = csv.HeaderRecord ?? Array.Empty<string>();

            var missingHeaders = requiredHeaders
                .Where(h => !headerRecord.Any(hr => hr.Equals(h, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missingHeaders.Any())
            {
                logger?.LogError("CSV missing required headers: {Headers}", string.Join(", ", missingHeaders));
                return false;
            }

            logger?.LogInformation("CSV headers validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "CSV header validation failed");
            return false;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}

/// <summary>
/// CsvHelper class map for CellularCoverageV2CsvRecord
/// Maps columns by index (no headers): Index 0=Lat, 1=Lng, 2=Signal, 3=Address
/// </summary>
internal sealed class CellularCoverageV2CsvMap : ClassMap<CellularCoverageV2CsvRecord>
{
    public CellularCoverageV2CsvMap()
    {
        Map(m => m.Latitude).Index(0);
        Map(m => m.Longitude).Index(1);
        Map(m => m.Signal).Index(2);
        Map(m => m.Address).Index(3);
    }
}
