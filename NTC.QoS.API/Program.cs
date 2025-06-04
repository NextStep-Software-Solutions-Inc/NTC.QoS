using NTC.QoS.Application;
using NTC.QoS.Infrastructure;
using System.Text.Json.Serialization;
using System.Text.Json;
using NTC.QoS.API.Extensions;
using Hangfire;
using Microsoft.AspNetCore.ResponseCompression;
using Clerk.Net.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.OpenApi.Models;

namespace NTC.QoS.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddClerkApiClient(config =>
        {
            config.SecretKey = builder.Configuration["Clerk:SecretKey"] ?? throw new InvalidOperationException("Clerk Secret Key is not configured.");
        });

        // JWT Bearer Authentication for Clerk.
        // Ensure these configuration values are correctly set in appsettings.json or environment variables.
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(x =>
        {
            // Authority is the URL of your clerk instance
            x.Authority = builder.Configuration["Clerk:Authority"];
            x.TokenValidationParameters = new TokenValidationParameters()
            {
                 // Disable audience validation as we aren't using it
                 ValidateAudience = false,
                 NameClaimType = ClaimTypes.NameIdentifier
            };
            x.Events = new JwtBearerEvents()
            {
                // Additional validation for AZP claim
                OnTokenValidated = context =>
                {
                     var azp = context.Principal?.FindFirstValue("azp");
                     var authorizedParties = builder.Configuration.GetSection("Clerk:AuthorizedParties").Get<List<string>>();

                     if (string.IsNullOrEmpty(azp) || authorizedParties == null || !authorizedParties.Contains(azp))
                     {
                         context.Fail("AZP Claim is invalid or missing");
                     }

                     return Task.CompletedTask;
                 }
            };
         });

        builder.Services.AddAuthorization();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        builder.Services.AddControllers()
            .AddJsonOptions(options => 
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; // Set to camelCase
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // Enable writing indented JSON for readability in development
                options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
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
        });
        builder.Services.AddApplication(builder.Configuration); // Call the Application registration
        builder.Services.AddInfrastructure(builder.Configuration); // Call the Infrastructure registration
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCaching();

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseCors();
            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                s.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
                s.ConfigObject.DeepLinking = true;
                s.ConfigObject.DisplayRequestDuration = true;
            });
        }

        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseResponseCaching();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard();

        app.ConfigureExceptionHandler(app.Logger); //Important line.
        app.MapControllers();

        app.Run();
    }
}

