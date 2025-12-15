using AspNetCoreRateLimit;
using Clerk.Net.DependencyInjection;
using NTC.QoS.API.Middleware;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NTC.QoS.API.Extensions;
using NTC.QoS.API.Filters;
using NTC.QoS.API.Infrastructure;
using NTC.QoS.Application;
using NTC.QoS.Infrastructure;
using NTC.QoS.Infrastructure.Extensions;
using NTC.QoS.Infrastructure.HealthChecks;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace NTC.QoS.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var env = builder.Environment;

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Configure Serilog for file-based logging
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/api-requests-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();


        // Validate Clerk secret for both Development and Production
        var clerkSecret = builder.Configuration["Clerk:SecretKey"] ?? throw new InvalidOperationException("Clerk Secret Key is not configured.");
        builder.Services.AddClerkApiClient(config =>
        {
            config.SecretKey = clerkSecret;
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(x =>
        {
            x.Authority = builder.Configuration["Clerk:Authority"];
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                NameClaimType = ClaimTypes.NameIdentifier
            };
            x.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var azp = context.Principal?.FindFirstValue("azp");
                    var authorizedParties = builder.Configuration.GetSection("Clerk:AuthorizedParties").Get<List<string>>();
                    if (!env.IsDevelopment())
                    {
                        if (string.IsNullOrEmpty(azp) || authorizedParties == null || !authorizedParties.Contains(azp))
                        {
                            context.Fail("AZP Claim is invalid or missing");
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        builder.Services.AddAuthorization();
        builder.Services.AddDynamicAuthorization();

        builder.Services.AddHttpContextAccessor();

        // CORS policies
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Dev", policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
            options.AddPolicy("Prod", policy =>
            {
                var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.WriteIndented = env.IsDevelopment();
            });

        // Configure file upload limits for large CSV files (streaming processing)
        builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 1073741824; // 1 GB - streaming handles large files efficiently
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
            options.BufferBodyLengthLimit = 134217728; // 128 MB buffer
        });

        builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 1073741824; // 1 GB
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5); // Allow more time for large uploads
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            // Include XML documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Please enter JWT token in the authentication header",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            options.AddSecurityDefinition("WebhookSignature", new OpenApiSecurityScheme
            {
                Name = "Webhook Signature",
                Description = "Clerk webhook signature verification (handled automatically)",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Webhook"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Custom operation filter for file upload endpoints
            options.OperationFilter<FileUploadOperationFilter>();
            options.OperationFilter<CamelCaseQueryParamsOperationFilter>();
            options.OperationFilter<SwaggerOperationIdFilter>();
            options.OperationFilter<CamelCaseOperationFilter>();
            options.OperationFilter<ClerkWebhookOperationFilter>();
        });

        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        // Add API-specific services
        builder.Services.AddApiRateLimiting();

        // Add verbose API logging services
        builder.Services.AddVerboseApiLogging(builder.Configuration);

        // Register Hangfire job server (required for processing jobs)
        builder.Services.AddHangfireServer();

        builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
                options.Providers.Add<BrotliCompressionProvider>();
            });
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCaching();

        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck<PendingMigrationsHealthCheck>("pending_migrations", failureStatus: HealthStatus.Degraded)
            .AddCheck<DbConnectionHealthCheck>("db");

        var app = builder.Build();

        // Initialize application startup processes (Hangfire filters, migrations, seeding)
        await app.Services.InitializeApplicationAsync(builder.Configuration, env);

        app.UseSecurityHeaders();
        
        // Enable verbose API request/response logging (should be early in pipeline)
        app.UseVerboseApiLogging();
        
        bool isDevelopment = app.Environment.IsDevelopment();
        app.UseCors(isDevelopment ? "Dev" : "Prod");

        if (isDevelopment)
        {
            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                s.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
                s.ConfigObject.DeepLinking = true;
                s.ConfigObject.DisplayRequestDuration = true;
            });
        }
        else
        {
            // Production security configuration (HSTS + HTTPS Redirection)
            app.UseProductionSecurity();
        }

        app.UseResponseCompression();
        app.UseResponseCaching();

        app.UseAuthentication();
        app.UseAuthorization();

        // Enable client-based rate limiting (per user)
        app.UseClientRateLimiting();


        // Hangfire dashboard with authorization
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter()]
        });

        app.ConfigureExceptionHandler(app.Logger);

        // Health endpoints
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        app.MapControllers();

        app.Run();
    }
}

