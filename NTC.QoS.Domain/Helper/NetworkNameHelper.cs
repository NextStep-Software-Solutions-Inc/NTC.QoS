namespace NTC.QoS.Domain.Helper;

public static class NetworkNameHelper
{
    public static string Normalize(string technologyStandard, string bandName, string municipalityName) =>
        $"{technologyStandard.Trim()}_{bandName.Trim()}_{municipalityName.Trim()}".ToLowerInvariant();
}
