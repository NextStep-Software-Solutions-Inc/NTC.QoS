using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.BackgroundTasks;
using NTC.QoS.Infrastructure.Extensions;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Persistence.Repositories;
using NTC.QoS.Infrastructure.Seed;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static async Task<IServiceCollection> AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        var hangfireConnectionString = configuration.GetConnectionString("HangfireConnection");

        services.AddLogging();

        services.AddHttpClient<IClerkUserService, ClerkUserService>(client =>
        {
            // BaseAddress and DefaultRequestHeaders should ideally be set here
            // to ensure the HttpClient instance injected into ClerkService is pre-configured.
            // This also ensures that the HttpClient is correctly managed by HttpClientFactory.
            client.BaseAddress = new Uri("https://api.clerk.com/v1/");
            // The API key will be added in the ClerkService constructor as it depends on IConfiguration.
            // For a more robust solution, consider passing Clerk API settings via options pattern.
        });

        // Register DbContext
        // Ensure IUserContextService is registered before DbContext if DbContext depends on it directly for interceptors.
        // It's generally better to register IUserContextService first and then resolve it.
        services.AddScoped<IUserContextService, UserContextService>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            // Resolve IUserContextService from the serviceProvider within the DbContext configuration
            var userContextService = serviceProvider.GetRequiredService<IUserContextService>();
            options.UseNpgsql(connectionString, x => x.UseNetTopologySuite()).AddInterceptors(new TimestampInterceptor(userContextService));
        });

        // Add dynamic authorization services
        services.AddDynamicAuthorization();

        // Add Hangfire services.
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConnectionString)) // Replace with your PostgreSQL connection string name
            .UseDashboardMetrics()
        );
        // Add the processing server as a hosted service
        services.AddHangfireServer(options =>
        {
            var configuredWorkerCount = configuration.GetValue<int?>("Hangfire:WorkerCount");
            // Use a conservative multiplier and cap the maximum worker count to avoid resource exhaustion
            const int maxWorkerCount = 40;
            int fallbackWorkerCount = Math.Min(Environment.ProcessorCount * 2, maxWorkerCount);
            options.WorkerCount = configuredWorkerCount ?? fallbackWorkerCount;
            // builder may not be available in this static context; derive a safe application name
            var appName = Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME")
                          ?? AppDomain.CurrentDomain.FriendlyName
                          ?? "HangfireServer";
            options.ServerName = $"{appName}-{Environment.MachineName}";
        });
        
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
        {
            Attempts = 3, // limit retries
            LogEvents = true,
            OnAttemptsExceeded = AttemptsExceededAction.Delete // or Fail/Succeed
        });

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IProviderRepository, ProviderRepository>();
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
        services.AddScoped<INAPRepository, NAPRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<ICellularRepository, CellularRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRegionRepository, UserRegionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IMIMORepository, MIMORepository>();
        services.AddScoped<ISectorRepository, SectorRepository>();
        services.AddScoped<IStudyNetworkRepository, StudyNetworkRepository>();
        services.AddScoped<IRegionRepository, RegionRepository>();
        services.AddScoped<IStudyMunicipalityRepository, StudyMunicipalityRepository>();
        services.AddScoped<ICellularCoverageRepository, CellularCoverageRepository>();
        services.AddScoped<ISystemTypeRepository, SystemTypeRepository>();
        services.AddScoped<ICellSectorRepository, CellSectorRepository>();
        services.AddScoped<ITechnologyBandRepository, TechnologyBandRepository>();

        services.AddTransient(typeof(IBackgroundJobService), typeof(BackgroundJobService));

        // Register all Jobs here
        services.AddTransient<IProcessNAPJob, ProcessNAPJob>();
        services.AddTransient<IProcessCellJob, ProcessCellJob>();
        services.AddTransient<IDeleteUploadedFileJob, DeleteUploadedFileJob>();
        services.AddTransient<IProcessSpeedtestJob, ProcessSpeedtestJob>();
        services.AddTransient<IProcessCellularCoverageJob, ProcessCellularCoverageJob>();

        await Task.Run(async () =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            // Try to get ClerkUserService, but don't fail if it's not available
            IClerkUserService? clerkUserService = null;
            try
            {
                clerkUserService = scope.ServiceProvider.GetService<IClerkUserService>();
            }
            catch (Exception ex)
            {
                logger.LogWarning("ClerkUserService not available during seeding: {Message}. User seeding will proceed without Clerk integration.", ex.Message);
            }

            // migrate first
            await dbContext.Database.MigrateAsync();
            await LocationSeeder.SeedAllAsync(dbContext, logger);
            await OrganizationSeeder.SeedAsync(dbContext, logger);
            await RoleSeeder.SeedAsync(dbContext, logger);
            await AuthorizationSeeder.SeedAsync(dbContext, logger);
            await UserSeeder.SeedAsync(dbContext, clerkUserService, configuration, logger);
        });

        return services;
    }
}
