using Microsoft.Extensions.Configuration;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Models;
using System.Text.Json;
using System.Web;

namespace NTC.QoS.Infrastructure.Services;

public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly string _geocodeApiKey;

    public GeocodingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _geocodeApiKey = configuration.GetSection("Geocode:SecretKey").Value ??
                         throw new InvalidOperationException("Geocode API Key is not configured.");
    }

    /// <summary>
    /// Gets the address details for given latitude and longitude coordinates.
    /// </summary>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longitude">The longitude.</param>
    /// <param name="format">Optional: The desired output format (e.g., "json", "jsonv2", "xml", "geojson"). Defaults to "jsonv2".</param>
    /// <returns>A string representing the formatted address, or null if not found.</returns>
    public async Task<string?> GetAddressByCoordinatesAsync(double latitude, double longitude, string format = "json")
    {
        string requestUri = $"https://geocode.maps.co/reverse?lat={latitude}&lon={longitude}&api_key={_geocodeApiKey}&format={format}";

        HttpResponseMessage response = await _httpClient.GetAsync(requestUri);

        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();

        if (format.ToLower() == "jsonv2" || format.ToLower() == "json")
        {
            try
            {
                // Deserialize the full response into our model
                var geocodingResponse = JsonSerializer.Deserialize<GeocodingReverseResponse>(responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Case-insensitive matching

                // Access the City property
                return geocodingResponse?.Address?.City;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing JSON response: {ex.Message}");
                // Optionally log the full responseBody for debugging
                // Console.WriteLine($"Response body: {responseBody}");
                return null;
            }
        }
        else if (format.ToLower() == "geojson" || format.ToLower() == "geocodejson")
        {
            // For GeoJSON/GeocodeJSON, return the raw string as it's a different structure.
            // You'd typically parse this with a GeoJSON library if you need specific properties.
            Console.WriteLine($"Warning: GetCityByCoordinatesAsync received GeoJSON/GeocodeJSON format. Returning raw response.");
            return responseBody;
        }
        else
        {
            Console.WriteLine($"Warning: GetCityByCoordinatesAsync received unhandled format '{format}'. Returning raw response.");
            return responseBody; // Return raw response for inspection by caller
        }
    }

    /// <summary>
    /// Gets the coordinates (latitude and longitude) for a given address.
    /// </summary>
    /// <param name="address">The address string to geocode.</param>
    /// <param name="format">Optional: The desired output format (e.g., "json", "jsonv2", "xml", "geojson"). Defaults to "jsonv2".</param>
    /// <returns>A tuple of (latitude, longitude) or null if coordinates are not found.</returns>
    public async Task<(double Latitude, double Longitude)?> GetCoordinatesByAddressAsync(string address, string format = "json")
    {
        string encodedAddress = HttpUtility.UrlEncode(address); // Or Uri.EscapeDataString(address)  
        string requestUri = $"https://geocode.maps.co/search?q={encodedAddress}&api_key={_geocodeApiKey}&format={format}";

        HttpResponseMessage response = await _httpClient.GetAsync(requestUri);

        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();

        if (format.ToLower() == "jsonv2" || format.ToLower() == "json")
        {
            try
            {
                // The search endpoint returns an array of results.  
                var searchResults = JsonSerializer.Deserialize<List<GeocodingForwardResponse>>(responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (searchResults != null && searchResults.Any())
                {
                    var firstResult = searchResults[0];
                    if (double.TryParse(firstResult.Lat, out double latitude) &&
                        double.TryParse(firstResult.Lon, out double longitude))
                    {
                        return (latitude, longitude);
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing JSON response for coordinates: {ex.Message}");
            }
        }

        return null;
    }
}
