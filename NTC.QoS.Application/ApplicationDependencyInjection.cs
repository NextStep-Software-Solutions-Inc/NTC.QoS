using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation.AspNetCore;
using FluentValidation;
using MediatR;
using System.Reflection;
using NTC.QoS.Application.Behaviors;
using NTC.QoS.Application.Contracts;
using NTC.QoS.Domain.Interfaces;

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

        // Register IFileProcessor implementations
        services.Scan(scan => scan
            .FromAssemblies(Assembly.GetExecutingAssembly())
            .AddClasses(classes => classes.AssignableTo<IFileProcessor>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan =>
            scan.FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo<IBackgroundJob>())
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        return services;
    }
}
