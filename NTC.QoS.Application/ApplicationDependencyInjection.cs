using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Application.Features.Provider.Handlers.Queries;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure;

namespace NTC.QoS.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration); // Call the Infrastructure registration
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAllProvidersHandler).Assembly));

        return services;
    }
}
