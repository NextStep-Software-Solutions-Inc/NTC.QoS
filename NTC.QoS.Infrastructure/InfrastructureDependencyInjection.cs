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
        services.AddScoped<ICellRepository, CellRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<IMunicipalityRepository, MunicipalityRepository>();
        services.AddScoped<IBarangayRepository, BarangayRepository>();
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<IStudyRepository, StudyRepository>();
        services.AddScoped<ITechnologyRepository, TechnologyRepository>();
        services.AddScoped<IBandRepository, BandRepository>();
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<IGenerationRepository, GenerationRepository>();
        services.AddScoped<IGenerationBandRepository, GenerationBandRepository>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<ICellularRepository, CellularRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

        return services;
    }
}
