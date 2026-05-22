namespace NTC.QoS.Domain.Helper;

/// <summary>
/// Provides frequency calculation utilities for cellular technology bands.
/// Used by manual cellular creation flows (CreateCellularHandler and UpdateCellTechnologiesBandHandler).
/// </summary>
public static class FrequencyHelper
{
    private const decimal DefaultDLCenterFreq = 0m;

    public static decimal GetDefaultDLCenterFreq(string technology, string band)
    {
        var normalizedBand = (band ?? string.Empty).Trim().ToUpperInvariant();
        return (technology.ToUpperInvariant(), normalizedBand) switch
        {
            ("GSM", "BAND 8") => 947.5m,
            ("GSM", "BAND 3") => 1842.5m,
            ("EDGE", "BAND 8") => 947.5m,
            ("EDGE", "BAND 3") => 1842.5m,
            ("WCDMA", "BAND 5") => 869m,
            ("WCDMA", "BAND 8") => 950m,
            ("WCDMA", "BAND 1") => 2140m,
            ("HSPAP", "BAND 5") => 869m,
            ("HSPAP", "BAND 8") => 950m,
            ("HSPAP", "BAND 1") => 2140m,
            ("FDDLTE", "BAND 28") => 758m,
            ("FDDLTE", "BAND 5") => 869m,
            ("FDDLTE", "BAND 8") => 950m,
            ("FDDLTE", "BAND 3") => 1842.5m,
            ("FDDLTE", "BAND 1") => 2140m,
            ("LTE", "BAND 28") => 758m,
            ("LTE", "BAND 5") => 869m,
            ("LTE", "BAND 8") => 950m,
            ("LTE", "BAND 3") => 1842.5m,
            ("LTE", "BAND 1") => 2140m,
            ("TDDLTE", "BAND 40") => 2300m,
            ("TDDLTE", "BAND 41") => 2500m,
            ("TDDLTE", "BAND 38") => 2600m,
            ("NR", "BAND 78") => 3500m,
            ("NR", "BAND 38") => 2600m,
            var (tech, bandName) when tech.Contains("TDD") => GetTDDDefaultFreq(bandName),
            var (tech, bandName) when tech.Contains("FDD") => GetFDDDefaultFreq(bandName),
            _ => DefaultDLCenterFreq
        };
    }

    public static decimal GetDefaultBandwidth(string technology)
    {
        return technology.ToUpperInvariant() switch
        {
            "GSM" => 0.2m,
            "EDGE" => 0.2m,
            "WCDMA" => 5m,
            "HSPAP" => 5m,
            "FDDLTE" => 20m,
            "TDDLTE" => 20m,
            "LTE" => 20m,
            "NR" => 100m,
            var tech when tech.Contains("TDD") => 20m,
            var tech when tech.Contains("FDD") => 20m,
            _ => 20m
        };
    }

    public static decimal CalculateULCenterFreq(decimal dlCenterFreq, string technology, string band)
    {
        var normalizedBand = (band ?? string.Empty).Trim().ToUpperInvariant();
        if (technology.Contains("TDD", StringComparison.OrdinalIgnoreCase) ||
            (technology.Equals("NR", StringComparison.OrdinalIgnoreCase) && IsTDDBand(normalizedBand)))
        {
            return dlCenterFreq;
        }

        return technology.ToUpperInvariant() switch
        {
            "GSM" => CalculateGsmULFreq(dlCenterFreq, normalizedBand),
            "EDGE" => CalculateGsmULFreq(dlCenterFreq, normalizedBand),
            "WCDMA" => CalculateWcdmaULFreq(dlCenterFreq, normalizedBand),
            "HSPAP" => CalculateWcdmaULFreq(dlCenterFreq, normalizedBand),
            "FDDLTE" => CalculateLteFddULFreq(dlCenterFreq, normalizedBand),
            "LTE" => CalculateLteFddULFreq(dlCenterFreq, normalizedBand),
            "TDDLTE" => dlCenterFreq,
            "NR" => CalculateNrFddULFreq(dlCenterFreq, normalizedBand),
            _ => dlCenterFreq
        };
    }

    private static bool IsTDDBand(string band)
    {
        var tddBands = new[] { "BAND 38", "BAND 40", "BAND 41", "BAND 42", "BAND 78" };
        return tddBands.Contains(band, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal GetTDDDefaultFreq(string band) => band switch
    {
        "BAND 40" => 2300m,
        "BAND 41" => 2500m,
        "BAND 38" => 2600m,
        "BAND 42" => 3400m,
        "BAND 78" => 3500m,
        _ => DefaultDLCenterFreq
    };

    private static decimal GetFDDDefaultFreq(string band) => band switch
    {
        "BAND 28" => 758m,
        "BAND 5" => 869m,
        "BAND 8" => 950m,
        "BAND 3" => 1842.5m,
        "BAND 1" => 2140m,
        _ => DefaultDLCenterFreq
    };

    private static decimal CalculateGsmULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "BAND 8" => dlCenterFreq - 45,
        "BAND 3" => dlCenterFreq - 95,
        _ => dlCenterFreq - 45
    };

    private static decimal CalculateWcdmaULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "BAND 5" => dlCenterFreq - 45,
        "BAND 8" => dlCenterFreq - 45,
        "BAND 1" => dlCenterFreq - 190,
        _ => dlCenterFreq - 190
    };

    private static decimal CalculateLteFddULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "BAND 28" => dlCenterFreq - 55,
        "BAND 5" => dlCenterFreq - 45,
        "BAND 8" => dlCenterFreq - 45,
        "BAND 3" => dlCenterFreq - 95,
        "BAND 1" => dlCenterFreq - 190,
        _ => dlCenterFreq - 45
    };

    private static decimal CalculateNrFddULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "BAND 28" => dlCenterFreq - 55,
        "BAND 1" => dlCenterFreq - 190,
        "BAND 3" => dlCenterFreq - 95,
        _ => dlCenterFreq - 45
    };
}
