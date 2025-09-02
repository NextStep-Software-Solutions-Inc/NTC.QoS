using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

public class ClerkUserService : IClerkUserService
{
    private readonly HttpClient _httpClient;
    private readonly string _clerkApiKey;

    public ClerkUserService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        // Use GetSection and Value for more robust configuration access.
        // Also, consider using `IClerkSettings` from options pattern if settings grow.
        _clerkApiKey = configuration.GetSection("Clerk:SecretKey").Value ?? throw new InvalidOperationException("Clerk API Key is not configured.");

        // HttpClient should ideally be configured once, typically in `AddHttpClient`.
        // Setting DefaultRequestHeaders here is generally fine if ClerkService is scoped.
        // For a more robust approach, use `HttpClientFactory` and named clients.
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _clerkApiKey);
        _httpClient.BaseAddress = new Uri("https://api.clerk.com/v1/"); // Set base address
    }

    public async Task<ClerkUserResponse> CreateUserAsync(CreateClerkUserPayload payload)
    {
        try
        {
            string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("users", content);

            // Directly check for success status code to throw HttpRequestException
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"User creation successful. Response: {responseBody}");

            var userResponse = JsonSerializer.Deserialize<ClerkUserResponse>(responseBody, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            return userResponse ?? throw new InvalidOperationException("Failed to deserialize Clerk user response.");
        }
        // Catch specific exceptions first
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue) // Check if StatusCode has a value
        {
            Console.Error.WriteLine($"HTTP Request Error creating user: Status Code - {ex.StatusCode}, Message - {ex.Message}");
            throw new InvalidOperationException($"Failed to create user in Clerk due to HTTP error.", ex);
        }
        catch (HttpRequestException ex) // General HttpRequestException
        {
            Console.Error.WriteLine($"HTTP Request Error creating user: {ex.Message}");
            throw new InvalidOperationException($"Failed to create user in Clerk due to HTTP error.", ex);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"JSON Serialization/Deserialization Error: {ex.Message}");
            throw new InvalidOperationException($"Failed to process JSON for user creation.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred in ClerkService.CreateUserAsync: {ex.Message}");
            throw new ApplicationException($"An unexpected error occurred while creating user.", ex);
        }
    }

    public async Task UpdateUserMetadataAsync(string userId, UserMetadataPayload payload)
    {
        try
        {
            string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PatchAsync($"users/{userId}/metadata", content);

            // Directly check for success status code to throw HttpRequestException
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Metadata update successful. Response: {responseBody}");
        }
        // Catch specific exceptions first
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue) // Check if StatusCode has a value
        {
            Console.Error.WriteLine($"HTTP Request Error for user {userId}: Status Code - {ex.StatusCode}, Message - {ex.Message}");
            // Depending on your error handling strategy, you might want to
            // return a specific error result or re-throw a custom exception.
            throw new InvalidOperationException($"Failed to update user metadata for user {userId} due to HTTP error.", ex);
        }
        catch (HttpRequestException ex) // General HttpRequestException
        {
            Console.Error.WriteLine($"HTTP Request Error for user {userId}: {ex.Message}");
            throw new InvalidOperationException($"Failed to update user metadata for user {userId} due to HTTP error.", ex);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"JSON Serialization/Deserialization Error: {ex.Message}");
            throw new InvalidOperationException($"Failed to process JSON for user {userId}.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred in ClerkService.UpdateUserMetadataAsync for user {userId}: {ex.Message}");
            // Avoid re-throwing generic Exception. Wrap it in a more specific exception.
            throw new ApplicationException($"An unexpected error occurred while updating user metadata for user {userId}.", ex);
        }
    }
}