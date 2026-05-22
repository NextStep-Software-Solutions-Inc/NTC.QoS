namespace NTC.QoS.Domain.Helper;

/// <summary>
/// Provides frequency calculation utilities for cellular technology bands.
/// Shared between background processing and manual creation flows.
/// </summary>
public static class FrequencyHelper
{
    private const decimal DefaultDLCenterFreq = 0m;

    public static decimal GetDefaultDLCenterFreq(string technology, string band)
    {
        return (technology.ToUpperInvariant(), band) switch
        {
            ("GSM", "Band 8") => 947.5m,
            ("GSM", "Band 3") => 1842.5m,
            ("EDGE", "Band 8") => 947.5m,
            ("EDGE", "Band 3") => 1842.5m,
            ("WCDMA", "Band 5") => 869m,
            ("WCDMA", "Band 8") => 950m,
            ("WCDMA", "Band 1") => 2140m,
            ("HSPAP", "Band 5") => 869m,
            ("HSPAP", "Band 8") => 950m,
            ("HSPAP", "Band 1") => 2140m,
            ("FDDLTE", "Band 28") => 758m,
            ("FDDLTE", "Band 5") => 869m,
            ("FDDLTE", "Band 8") => 950m,
            ("FDDLTE", "Band 3") => 1842.5m,
            ("FDDLTE", "Band 1") => 2140m,
            ("LTE", "Band 28") => 758m,
            ("LTE", "Band 5") => 869m,
            ("LTE", "Band 8") => 950m,
            ("LTE", "Band 3") => 1842.5m,
            ("LTE", "Band 1") => 2140m,
            ("TDDLTE", "Band 40") => 2300m,
            ("TDDLTE", "Band 41") => 2500m,
            ("TDDLTE", "Band 38") => 2600m,
            ("NR", "Band 78") => 3500m,
            ("NR", "Band 38") => 2600m,
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
        if (technology.Contains("TDD", StringComparison.OrdinalIgnoreCase) ||
            (technology.Equals("NR", StringComparison.OrdinalIgnoreCase) && IsTDDBand(band)))
        {
            return dlCenterFreq;
        }

        return technology.ToUpperInvariant() switch
        {
            "GSM" => CalculateGsmULFreq(dlCenterFreq, band),
            "EDGE" => CalculateGsmULFreq(dlCenterFreq, band),
            "WCDMA" => CalculateWcdmaULFreq(dlCenterFreq, band),
            "HSPAP" => CalculateWcdmaULFreq(dlCenterFreq, band),
            "FDDLTE" => CalculateLteFddULFreq(dlCenterFreq, band),
            "LTE" => CalculateLteFddULFreq(dlCenterFreq, band),
            "TDDLTE" => dlCenterFreq,
            "NR" => CalculateNrFddULFreq(dlCenterFreq, band),
            _ => dlCenterFreq
        };
    }

    private static bool IsTDDBand(string band)
    {
        var tddBands = new[] { "Band 38", "Band 40", "Band 41", "Band 42", "Band 78" };
        return tddBands.Contains(band, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal GetTDDDefaultFreq(string band) => band switch
    {
        "Band 40" => 2300m,
        "Band 41" => 2500m,
        "Band 38" => 2600m,
        "Band 42" => 3400m,
        "Band 78" => 3500m,
        _ => DefaultDLCenterFreq
    };

    private static decimal GetFDDDefaultFreq(string band) => band switch
    {
        "Band 28" => 758m,
        "Band 5" => 869m,
        "Band 8" => 950m,
        "Band 3" => 1842.5m,
        "Band 1" => 2140m,
        _ => DefaultDLCenterFreq
    };

    private static decimal CalculateGsmULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "Band 8" => dlCenterFreq - 45,
        "Band 3" => dlCenterFreq - 95,
        _ => dlCenterFreq - 45
    };

    private static decimal CalculateWcdmaULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "Band 5" => dlCenterFreq - 45,
        "Band 8" => dlCenterFreq - 45,
        "Band 1" => dlCenterFreq - 190,
        _ => dlCenterFreq - 190
    };

    private static decimal CalculateLteFddULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "Band 28" => dlCenterFreq - 55,
        "Band 5" => dlCenterFreq - 45,
        "Band 8" => dlCenterFreq - 45,
        "Band 3" => dlCenterFreq - 95,
        "Band 1" => dlCenterFreq - 190,
        _ => dlCenterFreq - 45
    };

    private static decimal CalculateNrFddULFreq(decimal dlCenterFreq, string band) => band switch
    {
        "Band 28" => dlCenterFreq - 55,
        "Band 1" => dlCenterFreq - 190,
        "Band 3" => dlCenterFreq - 95,
        _ => dlCenterFreq - 45
    };
}
