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

        // Scheduler service
        services.AddScoped<ISchedulerService, SchedulerService>();

        // Assessment consolidation service (singleton for in-memory consolidation across requests)
        services.AddSingleton<IAssessmentConsolidationService, AssessmentConsolidationService>();

        // Job implementations
        services.AddTransient<IProcessNAPJob, ProcessNAPJob>();
        services.AddTransient<IProcessCellJob, ProcessCellJob>();
        services.AddTransient<IDeleteUploadedFileJob, DeleteUploadedFileJob>();
        services.AddTransient<IProcessSpeedtestJob, ProcessSpeedtestJob>();
        services.AddTransient<IProcessCellularCoverageJob, ProcessCellularCoverageJob>();
        services.AddTransient<IProcessCellularCoverageV2Job, ProcessCellularCoverageV2Job>();
        services.AddTransient<IProcessConnectivityAssessmentJob, ProcessConnectivityAssessmentJob>();
        services.AddTransient<IProcessWirelessValidationJob, ProcessWirelessValidationJob>();
        services.AddTransient<IProcessWirelessBroadbandReportJob, ProcessWirelessBroadbandReportJob>();
        services.AddTransient<IProcessWiredBroadbandReportJob, ProcessWiredBroadbandReportJob>();
        services.AddTransient<IProcessConnectivityGapReportJob, ProcessConnectivityGapReportJob>();
        services.AddTransient<IProcessConnectivityReportJob, ProcessConnectivityReportJob>();
        
        // New job for triggering assessment generation
        services.AddTransient<IGenerateConnectivityAssessmentJob, GenerateConnectivityAssessmentJob>();
        
        // Scheduled job wrapper with date validation
        services.AddTransient<IScheduledConnectivityAssessmentJob, ScheduledConnectivityAssessmentJob>();

        return services;
    }
}