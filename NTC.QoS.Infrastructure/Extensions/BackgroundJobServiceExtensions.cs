using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.BackgroundTasks;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for background job service registration
/// </summary>
internal static class BackgroundJobServiceExtensions
{
    /// <summary>
    /// Registers background job services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBackgroundJobServices(this IServiceCollection services)
    {
        // Background job service
        services.AddTransient<IBackgroundJobService, BackgroundJobService>();

        // Job implementations
        services.AddTransient<IProcessNAPJob, ProcessNAPJob>();
        services.AddTransient<IProcessCellJob, ProcessCellJob>();
        services.AddTransient<IDeleteUploadedFileJob, DeleteUploadedFileJob>();
        services.AddTransient<IProcessSpeedtestJob, ProcessSpeedtestJob>();
        services.AddTransient<IProcessCellularCoverageJob, ProcessCellularCoverageJob>();
        services.AddTransient<IProcessConnectivityAssessmentJob, ProcessConnectivityAssessmentJob>();
        services.AddTransient<IProcessWirelessValidationJob, ProcessWirelessValidationJob>();

        return services;
    }
}