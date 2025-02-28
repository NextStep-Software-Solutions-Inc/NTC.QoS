using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Persistence.Repositories;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure;
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");

        // Register DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var userContextService = services.BuildServiceProvider().GetRequiredService<IUserContextService>();
            options.UseNpgsql(connectionString).AddInterceptors(new TimestampInterceptor(userContextService));
        });

        // Register repositories

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IProvinceRepository, ProvinceRepository>();
        services.AddScoped<IServiceProviderRepository, ServiceProviderRepository>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<IMunicipalityRepository, MunicipalityRepository>();

        return services;
    }
}
