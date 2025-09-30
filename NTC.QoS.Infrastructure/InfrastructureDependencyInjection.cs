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
    // Pure registration only. Seeding/migrations moved to ApplyMigrationsAndSeedAsync
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        var hangfireConnectionString = configuration.GetConnectionString("HangfireConnection");

        services.AddLogging();

        services.AddHttpClient<IClerkUserService, ClerkUserService>(client =>
        {
            client.BaseAddress = new Uri("https://api.clerk.com/v1/");
        });

        services.AddScoped<IUserContextService, UserContextService>();
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var audit = sp.GetRequiredService<AuditInterceptor>();
            var softDelete = sp.GetRequiredService<SoftDeleteInterceptor>();
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseNetTopologySuite();
                    npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                })
                .AddInterceptors(audit, softDelete);
        });

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(s => s.UseNpgsqlConnection(hangfireConnectionString))
            .UseDashboardMetrics()
        );

        services.AddHangfireServer(options =>
        {
            var configuredWorkerCount = configuration.GetValue<int?>("Hangfire:WorkerCount");
            const int maxWorkerCount = 32;
            int fallback = Math.Min(Environment.ProcessorCount, maxWorkerCount);
            options.WorkerCount = configuredWorkerCount is > 0 ? Math.Min(configuredWorkerCount.Value, maxWorkerCount) : fallback;
            var appName = Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME")
                          ?? AppDomain.CurrentDomain.FriendlyName
                          ?? "HangfireServer";
            options.ServerName = $"{appName}-{Environment.MachineName}";
        });

        // Hangfire global filter registrar
        services.AddSingleton<IHangfireFilterRegistrar, HangfireFilterRegistrar>();

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

        services.AddTransient<IBackgroundJobService, BackgroundJobService>();
        services.AddTransient<IProcessNAPJob, ProcessNAPJob>();
        services.AddTransient<IProcessCellJob, ProcessCellJob>();
        services.AddTransient<IDeleteUploadedFileJob, DeleteUploadedFileJob>();
        services.AddTransient<IProcessSpeedtestJob, ProcessSpeedtestJob>();
        services.AddTransient<IProcessCellularCoverageJob, ProcessCellularCoverageJob>();

        return services;
    }

    // Explicit migration / seeding entry point. Call once after building the app.
    public static async Task ApplyMigrationsAndSeedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Acquire PostgreSQL advisory lock to prevent concurrent migrations/seeding across instances
        const long advisoryLockKey = 987654321012345678; // Arbitrary unique bigint key
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({advisoryLockKey});", cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
            await LocationSeeder.SeedAllAsync(dbContext, logger);
            await OrganizationSeeder.SeedAsync(dbContext, logger);
            await RoleSeeder.SeedAsync(dbContext, logger);
            await AuthorizationSeeder.SeedAsync(dbContext, logger);
        }
        finally
        {
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({advisoryLockKey});", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to release advisory lock {LockKey}", advisoryLockKey);
            }
        }
    }
}
