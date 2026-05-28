using System.Text.RegularExpressions;

namespace NTC.QoS.Domain.Helper;

public static partial class NetworkNameHelper
{
    /// <summary>
    /// Builds a normalized network name from its component parts (technology, band, municipality).
    /// Used by flows that construct network names from individual fields.
    /// </summary>
    public static string Normalize(string technologyStandard, string bandName, string municipalityName) =>
        NormalizeStoredName($"{technologyStandard}_{bandName}_{municipalityName}");

    /// <summary>
    /// Normalizes an already-composed network name to match the DB storage convention:
    /// trims whitespace, collapses whitespace around underscores, and lowercases.
    /// Matches the migration logic: <c>lower(regexp_replace(btrim(Name), '\s*_\s*', '_', 'g'))</c>.
    /// </summary>
    public static string NormalizeStoredName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return UnderscoreWhitespace().Replace(name.Trim(), "_").ToLowerInvariant();
    }

    [GeneratedRegex(@"\s*_\s*")]
    private static partial Regex UnderscoreWhitespace();
}
