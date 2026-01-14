using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Application.Behaviors;
using NTC.QoS.Application.Services;
using NTC.QoS.Application.Services.MCP;
using NTC.QoS.Domain.Interfaces;
using System.Reflection;

namespace NTC.QoS.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationAssembly = typeof(ApplicationDependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            cfg.Lifetime = ServiceLifetime.Scoped;
        });
        services.AddFluentValidationAutoValidation();
        //services.AddValidatorsFromAssembly(applicationAssembly); // This is what I used before FluentValidation.AspNetCore but it's not needed anymore since im using Scutor scanning to register validators in the next line.  
        services.Scan(scan => scan
            .FromAssemblies(Assembly.GetExecutingAssembly())
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.Scan(scan =>
            scan.FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo(typeof(IBackgroundJob<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        // Register Application Services
        services.AddScoped<IConnectivityCacheService, ConnectivityCacheService>();
        services.AddScoped<IConnectivityReportingService, ConnectivityReportingService>();
        services.AddScoped<BroadbandAnalyticsService>();
        
        // MCP (Model Context Protocol) Server service for AI database access
        services.AddScoped<IMCPServerService, MCPServerService>();

        // Configure ConnectivityCache options
        services.Configure<ConnectivityCacheOptions>(
            configuration.GetSection("ConnectivityCache"));

        return services;
    }
}
