namespace NTC.QoS.Domain.Interfaces;

public interface IGeocodingService
{
    Task<string?> GetAddressByCoordinatesAsync(double latitude, double longitude, string format = "json");
    Task<(double Latitude, double Longitude)?> GetCoordinatesByAddressAsync(string address, string format = "json");
}
