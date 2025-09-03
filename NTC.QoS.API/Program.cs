using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.ResponseCompression;
using NTC.QoS.API.Extensions;
using NTC.QoS.Application;
using NTC.QoS.Application.Services;
using NTC.QoS.Infrastructure;
using Clerk.Net.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using NTC.QoS.API.Filters;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NTC.QoS.Infrastructure.HealthChecks;
using NTC.QoS.API.Infrastructure;

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

        // Validate Clerk secret strictly only outside Development
        var clerkSecret = builder.Configuration["Clerk:SecretKey"];
        if (!env.IsDevelopment() && string.IsNullOrWhiteSpace(clerkSecret))
        {
            throw new InvalidOperationException("Clerk Secret Key is not configured.");
        }

        builder.Services.AddClerkApiClient(config =>
        {
            config.SecretKey = clerkSecret ?? "dev_placeholder_secret"; // allow placeholder in dev
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

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Please enter JWT token in the authentication header",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
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
            options.OperationFilter<CamelCaseQueryParamsOperationFilter>();
            options.OperationFilter<SwaggerOperationIdFilter>();
        });

        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCaching();
        builder.Services.AddScoped<GeospatialQueryService>();

        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck<PendingMigrationsHealthCheck>("pending_migrations", failureStatus: HealthStatus.Degraded)
            .AddCheck<DbConnectionHealthCheck>("db");

        var app = builder.Build();
        
        // Conditional migrations & seeding
        var runMigrations = builder.Configuration.GetValue<bool?>("Database:RunMigrationsOnStartup") ?? env.IsDevelopment();
        if (runMigrations)
        {
            await app.Services.ApplyMigrationsAndSeedAsync();
        }

        // Security headers (minimal baseline)
        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers.Referer = "no-referrer";
            context.Response.Headers.XXSSProtection = "0"; // modern browsers ignore, set explicit
            if (!env.IsDevelopment())
            {
                context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            }
            await next();
        });

        app.UseCors(env.IsDevelopment() ? "Dev" : "Prod");

        if (env.IsDevelopment())
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
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseResponseCaching();

        app.UseAuthentication();
        app.UseAuthorization();

        // Conditional Hangfire dashboard exposure
        if (env.IsDevelopment())
        {
            app.UseHangfireDashboard();
        }
        else
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()]
            });
        }

        app.ConfigureExceptionHandler(app.Logger);

        // Health endpoints
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        app.MapControllers();

        app.Run();
    }
}

