using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NTC.QoS.Application.Features.Scheduler.Requests.Commands;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Application.Features.Scheduler.Handlers.Commands;

/// <summary>
/// Handler for creating a new connectivity assessment schedule
/// </summary>
public class CreateSchedulerHandler : IRequestHandler<CreateSchedulerRequest, SchedulerResponseDto>
{
    private readonly IConnectivityAssessmentSchedulerRepository _repository;
    private readonly ISchedulerService _schedulerService;
    private readonly ILogger<CreateSchedulerHandler> _logger;

    public CreateSchedulerHandler(
        IConnectivityAssessmentSchedulerRepository repository,
        ISchedulerService schedulerService,
        ILogger<CreateSchedulerHandler> logger)
    {
        _repository = repository;
        _schedulerService = schedulerService;
        _logger = logger;
    }

    public async Task<SchedulerResponseDto> Handle(CreateSchedulerRequest request, CancellationToken cancellationToken)
    {
        var dto = request.Data;

        // Validate timezone FIRST to prevent runtime errors
        if (!_schedulerService.ValidateTimeZone(dto.TimeZone))
        {
            _logger.LogError("Invalid timezone: {TimeZone}", dto.TimeZone);
            throw new ArgumentException($"Invalid timezone: {dto.TimeZone}. Use IANA timezone IDs like 'Asia/Manila' or Windows IDs like 'Singapore Standard Time'");
        }

        // Generate unique Hangfire job ID
        var hangfireJobId = $"ConnectivityAssessment_{Guid.NewGuid():N}";

        // Generate cron expression
        var cronExpression = _schedulerService.GenerateCronExpression(dto.Configuration, dto.Frequency);

        // Calculate next execution time
        var nextExecutionTime = _schedulerService.GetNextExecutionTime(cronExpression, dto.TimeZone);

        _logger.LogInformation("Creating scheduler '{Name}' with cron '{Cron}', TimeZone: {TimeZone}, Next run: {NextRun}",
            dto.Name, cronExpression, dto.TimeZone, nextExecutionTime);

        // Create the scheduler entity
        var scheduler = new ConnectivityAssessmentScheduler
        {
            HangfireJobId = hangfireJobId,
            Name = dto.Name,
            Description = dto.Description,
            Frequency = dto.Frequency,
            CronExpression = cronExpression,
            TimeZone = dto.TimeZone,
            Status = SchedulerStatusEnum.Active,
            LocationType = dto.LocationType,
            LocationIdsJson = JsonConvert.SerializeObject(dto.LocationIds),
            NextExecutionTime = nextExecutionTime,
            SendNotifications = dto.SendNotifications,
            NotificationEmailsJson = dto.NotificationEmails != null ? JsonConvert.SerializeObject(dto.NotificationEmails) : null,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ConfigurationJson = JsonConvert.SerializeObject(new JobExecutionConfigurationDto
            {
                GenerateHierarchicalAssessments = dto.GenerateHierarchicalAssessments,
                HierarchicalDepth = dto.HierarchicalDepth,
                MaxHierarchicalLocations = dto.MaxHierarchicalLocations,
                ForceRecalculate = dto.ForceRecalculate,
                ScheduleConfiguration = dto.Configuration
            })
        };

        // Save to database
        await _repository.AddAsync(scheduler, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Register with Hangfire IMMEDIATELY after DB save
        // This prevents the "nextExecutionTime is null" issue
        await _schedulerService.RegisterSchedulerAsync(scheduler);
        
        // Recalculate next execution time after Hangfire registration
        // This ensures it's set even if there's a timing issue
        scheduler.NextExecutionTime = _schedulerService.GetNextExecutionTime(cronExpression, dto.TimeZone);
        _repository.Update(scheduler);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduler '{Name}' (ID: {Id}) created and registered successfully. Next execution: {NextExecution}",
            scheduler.Name, scheduler.Id, scheduler.NextExecutionTime);

        // Return response
        return MapToResponseDto(scheduler, _schedulerService);
    }

    private static SchedulerResponseDto MapToResponseDto(ConnectivityAssessmentScheduler scheduler, ISchedulerService schedulerService)
    {
        var config = string.IsNullOrEmpty(scheduler.ConfigurationJson)
            ? new JobExecutionConfigurationDto()
            : JsonConvert.DeserializeObject<JobExecutionConfigurationDto>(scheduler.ConfigurationJson) ?? new JobExecutionConfigurationDto();

        return new SchedulerResponseDto
        {
            Id = scheduler.Id,
            HangfireJobId = scheduler.HangfireJobId,
            Name = scheduler.Name,
            Description = scheduler.Description,
            Frequency = scheduler.Frequency,
            CronExpression = scheduler.CronExpression,
            TimeZone = scheduler.TimeZone,
            Status = scheduler.Status,
            LocationType = scheduler.LocationType,
            LocationIds = JsonConvert.DeserializeObject<List<Guid>>(scheduler.LocationIdsJson) ?? new List<Guid>(),
            LastExecutionTime = scheduler.LastExecutionTime,
            NextExecutionTime = scheduler.NextExecutionTime,
            ExecutionCount = scheduler.ExecutionCount,
            FailureCount = scheduler.FailureCount,
            LastError = scheduler.LastError,
            SendNotifications = scheduler.SendNotifications,
            NotificationEmails = scheduler.NotificationEmailsJson != null 
                ? JsonConvert.DeserializeObject<List<string>>(scheduler.NotificationEmailsJson) 
                : null,
            StartDate = scheduler.StartDate,
            EndDate = scheduler.EndDate,
            Configuration = config.ScheduleConfiguration,
            GenerateHierarchicalAssessments = config.GenerateHierarchicalAssessments,
            HierarchicalDepth = config.HierarchicalDepth,
            MaxHierarchicalLocations = config.MaxHierarchicalLocations,
            ForceRecalculate = config.ForceRecalculate,
            CreatedAt = scheduler.CreatedAt,
            CreatedBy = scheduler.CreatedBy,
            UpdatedAt = scheduler.UpdatedAt,
            UpdatedBy = scheduler.UpdatedBy
        };
    }
}
