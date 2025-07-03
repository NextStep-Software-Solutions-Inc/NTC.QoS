using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.ResponseCompression;
using NTC.QoS.API.Extensions;
using NTC.QoS.Application;
using NTC.QoS.Infrastructure;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.OperationFilter<SwaggerOperationIdFilter>();
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

var app = builder.Build();

// ✅ Async seeding logic
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await LocationSeeder.SeedAllAsync(dbContext, logger);
}

// Configure middleware
if (app.Environment.IsDevelopment())
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

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseResponseCaching();

app.UseCors();
app.UseAuthorization();
app.UseHangfireDashboard();
app.ConfigureExceptionHandler(app.Logger);

app.MapControllers();

await app.RunAsync();
