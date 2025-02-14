using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Persistence.Repositories;

namespace NTC.QoS.Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgreSqlConnection");

            // Register DbContext
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

            // Register repositories
            services.AddScoped<IProviderRepository, ProviderRepository>();

            return services;
        }
    }
}