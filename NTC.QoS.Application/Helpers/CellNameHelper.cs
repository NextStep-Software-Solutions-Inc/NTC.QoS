namespace NTC.QoS.Application.Helpers;

public static class CellNameHelper
{
    public static string BuildTemplateName(string serviceProviderName, string? address, string towerType, double longitude, double latitude)
    {
        return $"Cell-{serviceProviderName}-{address ?? string.Empty}-{towerType}-{longitude}-{latitude}";
    }

    public static bool IsTemplateName(string? currentName, string serviceProviderName, string? address, string towerType, double longitude, double latitude)
    {
        return string.Equals(
            currentName,
            BuildTemplateName(serviceProviderName, address, towerType, longitude, latitude),
            StringComparison.Ordinal);
    }
}
