namespace NTC.QoS.API.Middleware;

/// <summary>
/// Middleware that adds security headers to HTTP responses.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        // Continue to the next middleware
        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        // Prevent MIME type sniffing
        response.Headers.XContentTypeOptions = "nosniff";

        // Prevent the page from being displayed in a frame
        response.Headers.XFrameOptions = "DENY";

        // Control referrer information
        response.Headers["Referrer-Policy"] = "no-referrer";

        // Disable XSS protection (modern browsers ignore this, but set explicitly)
        response.Headers.XXSSProtection = "0";

        // Add HSTS header only in production
        if (!_environment.IsDevelopment())
        {
            response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
        }
    }
}