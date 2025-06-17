using NTC.QoS.Application;
using NTC.QoS.Infrastructure;
using System.Text.Json.Serialization;
using System.Text.Json;
using NTC.QoS.API.Extensions;
using Hangfire;
using Microsoft.AspNetCore.ResponseCompression;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seeder;
using Microsoft.EntityFrameworkCore;

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

        app.UseAuthorization();
        app.UseHangfireDashboard();

        app.ConfigureExceptionHandler(app.Logger);
        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<ApplicationDbContext>();

                Console.WriteLine("Applying database migrations (if any)...");
                dbContext.Database.MigrateAsync().GetAwaiter().GetResult();
                Console.WriteLine("Database migrations applied.");

                Console.WriteLine("Starting database seeding process...");
                DatabaseSeeder.SeedEntityAsync(dbContext, LocationTypeToSeedEnum.Region).GetAwaiter().GetResult();
                DatabaseSeeder.SeedEntityAsync(dbContext, LocationTypeToSeedEnum.Province).GetAwaiter().GetResult();

                Console.WriteLine("Database seeding process completed.");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        app.Run();
    }
}

