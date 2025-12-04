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
