using Hangfire;
using MediatR;
using Microsoft.AspNetCore.ResponseCompression;
using NTC.QoS.API.Extensions;
using NTC.QoS.Application;
using NTC.QoS.Application.Behaviors;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NTC.QoS.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
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
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(option =>
        {
            option.OperationFilter<SwaggerOperationIdFilter>();
        });

        // Add MediatR registration FIRST, before your custom behaviors
        // Register MediatR and specify the assembly where your handlers/requests are.
        // Assuming your MediatR requests and handlers are defined within the NTC.QoS.Application project.
        builder.Services.AddMediatR(cfg =>
        {
            // Register services from the assembly containing your MediatR requests/handlers.
            // Using a known request type to get the assembly reference.
            cfg.RegisterServicesFromAssembly(typeof(GetTotalCellsitesCountRequest).Assembly);

            // Register your CachingBehavior as a pipeline behavior.
            // This order is important: behaviors are executed in the order they are registered.
            // Typically, caching behavior goes early in the pipeline to prevent further processing on a cache hit.
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

            // If you have other behaviors (e.g., validation behavior),
            // you'd add them here as well. The order matters.
            // Example: cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
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
        builder.Services.AddScoped<GeospatialQueryService>();

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

        app.UseAuthorization();
        app.UseHangfireDashboard();

        app.ConfigureExceptionHandler(app.Logger); //Important line.
        app.MapControllers();

        app.Run();
    }
}

