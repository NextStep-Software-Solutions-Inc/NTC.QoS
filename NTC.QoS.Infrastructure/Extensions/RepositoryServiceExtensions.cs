using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence.Repositories;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for repository service registration
/// </summary>
internal static class RepositoryServiceExtensions
{
    /// <summary>
    /// Registers all repository services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Entity-specific repositories
        RegisterEntityRepositories(services);
        RegisterLocationRepositories(services);
        RegisterNetworkRepositories(services);
        RegisterUserManagementRepositories(services);
        RegisterTelecommunicationRepositories(services);

        return services;
    }

    private static void RegisterEntityRepositories(IServiceCollection services)
    {
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceProviderRepository, ServiceProviderRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IStudyRepository, StudyRepository>();
        services.AddScoped<ITechnologyRepository, TechnologyRepository>();
        services.AddScoped<IBandRepository, BandRepository>();
        services.AddScoped<IGenerationRepository, GenerationRepository>();
        services.AddScoped<IGenerationBandRepository, GenerationBandRepository>();
        services.AddScoped<ITechnologyBandRepository, TechnologyBandRepository>();
        services.AddScoped<ISystemTypeRepository, SystemTypeRepository>();
        services.AddScoped<IGeneratedReportRepository, GeneratedReportRepository>();
    }

    private static void RegisterLocationRepositories(IServiceCollection services)
    {
        services.AddScoped<IRegionRepository, RegionRepository>();
        services.AddScoped<IProvinceRepository, ProvinceRepository>();
        services.AddScoped<IMunicipalityRepository, MunicipalityRepository>();
        services.AddScoped<IBarangayRepository, BarangayRepository>();
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<IStudyMunicipalityRepository, StudyMunicipalityRepository>();
    }

    private static void RegisterNetworkRepositories(IServiceCollection services)
    {
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ICellRepository, CellRepository>();
        services.AddScoped<ISectorRepository, SectorRepository>();
        services.AddScoped<ICellSectorRepository, CellSectorRepository>();
        services.AddScoped<IStudyNetworkRepository, StudyNetworkRepository>();
        services.AddScoped<IMIMORepository, MIMORepository>();
    }

    private static void RegisterUserManagementRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRegionRepository, UserRegionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
    }

    private static void RegisterTelecommunicationRepositories(IServiceCollection services)
    {
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<INAPRepository, NAPRepository>();
        services.AddScoped<ICellularRepository, CellularRepository>();
        services.AddScoped<ICellularCoverageRepository, CellularCoverageRepository>();
        services.AddScoped<ICellularCoverageV2Repository, CellularCoverageV2Repository>();
        services.AddScoped<IConnectivityAssessmentRepository, ConnectivityAssessmentRepository>();
        services.AddScoped<IConnectivityAssessmentSchedulerRepository, ConnectivityAssessmentSchedulerRepository>();
        services.AddScoped<IWiFiHotspotRepository, WiFiHotspotRepository>();
        services.AddScoped<IWirelessValidationRepository, WirelessValidationRepository>();
    }
}