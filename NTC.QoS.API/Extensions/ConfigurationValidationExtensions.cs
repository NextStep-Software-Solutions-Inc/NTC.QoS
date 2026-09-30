using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NTC.QoS.API.Extensions;

/// <summary>
/// Provides extension methods for validating critical application configuration at startup,
/// implementing fail-fast safeguards to prevent runtime failures.
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Validates all required Clerk and authentication environment configurations at application startup.
    /// Throws an <see cref="InvalidOperationException"/> with detailed error diagnostics if any required setting is missing or malformed.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <param name="environment">The hosting environment</param>
    public static void ValidateClerkConfiguration(this IConfiguration configuration, IHostEnvironment environment)
    {
        var errors = new List<string>();

        // 1. Clerk:SecretKey
        var secretKey = configuration["Clerk:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            errors.Add("Clerk:SecretKey is required but missing. Please configure a valid Clerk secret key (must start with 'sk_').");
        }
        else if (!secretKey.StartsWith("sk_"))
        {
            errors.Add("Clerk:SecretKey is malformed. A valid Clerk secret key must begin with 'sk_'.");
        }

        // 2. Clerk:Authority
        var authority = configuration["Clerk:Authority"];
        Uri? authorityUri = null;
        if (string.IsNullOrWhiteSpace(authority))
        {
            errors.Add("Clerk:Authority is required but missing. Please configure the Clerk issuer authority URI (e.g. 'https://<app>.clerk.accounts.dev').");
        }
        else if (!Uri.TryCreate(authority, UriKind.Absolute, out authorityUri) || 
                 (authorityUri.Scheme != Uri.UriSchemeHttp && authorityUri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add($"Clerk:Authority '{authority}' is not a valid absolute HTTP/HTTPS URI.");
        }

        // 3. Clerk:WebhookSecret (Svix)
        var webhookSecret = configuration["Clerk:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            if (!environment.IsDevelopment())
            {
                errors.Add("Clerk:WebhookSecret is required for webhook signature verification but missing. Please configure 'Clerk:WebhookSecret' (must start with 'whsec_').");
            }
        }
        else if (!webhookSecret.StartsWith("whsec_"))
        {
            errors.Add("Clerk:WebhookSecret is malformed. A valid Svix webhook secret must begin with 'whsec_'.");
        }

        // 4. Clerk:FrontendUrl / AuthorizedParties
        var frontendUrl = configuration["Clerk:FrontendUrl"];
        var authorizedParties = configuration.GetSection("Clerk:AuthorizedParties").Get<string[]>() ?? [];
        if (string.IsNullOrWhiteSpace(frontendUrl) && authorizedParties.Length == 0)
        {
            errors.Add("Neither 'Clerk:FrontendUrl' nor 'Clerk:AuthorizedParties' is configured. At least one frontend URL is required for user invitations and redirect routing.");
        }
        else if (!string.IsNullOrWhiteSpace(frontendUrl) && !Uri.TryCreate(frontendUrl, UriKind.Absolute, out _))
        {
            errors.Add($"Clerk:FrontendUrl '{frontendUrl}' is not a valid absolute URI.");
        }

        // 5. Non-development environment HTTPS safeguards
        if (!environment.IsDevelopment())
        {
            if (authorityUri != null && authorityUri.Scheme != Uri.UriSchemeHttps)
            {
                errors.Add("In non-Development environments, Clerk:Authority must use HTTPS.");
            }

            if (!string.IsNullOrWhiteSpace(frontendUrl) && 
                Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontUri) && 
                frontUri.Scheme != Uri.UriSchemeHttps)
            {
                errors.Add("In non-Development environments, Clerk:FrontendUrl must use HTTPS.");
            }
        }

        if (errors.Count > 0)
        {
            var message = $"[CRITICAL STARTUP CONFIGURATION ERROR] The application cannot start due to {errors.Count} missing or invalid configuration setting(s):\n" +
                          string.Join("\n", errors.Select((e, i) => $"  {i + 1}. {e}"));
            throw new InvalidOperationException(message);
        }
    }
}
