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
/// Handler for updating an existing connectivity assessment schedule
/// </summary>
public class UpdateSchedulerHandler : IRequestHandler<UpdateSchedulerRequest, SchedulerResponseDto>
{
    private readonly IConnectivityAssessmentSchedulerRepository _repository;
    private readonly ISchedulerService _schedulerService;
    private readonly ILogger<UpdateSchedulerHandler> _logger;

    public UpdateSchedulerHandler(
        IConnectivityAssessmentSchedulerRepository repository,
        ISchedulerService schedulerService,
        ILogger<UpdateSchedulerHandler> logger)
    {
        _repository = repository;
        _schedulerService = schedulerService;
        _logger = logger;
    }

    public async Task<SchedulerResponseDto> Handle(UpdateSchedulerRequest request, CancellationToken cancellationToken)
    {
        var dto = request.Data;

        // Get existing scheduler
        var query = _repository.Query(s => s.Id == request.Id);
        var scheduler = await _repository.FirstOrDefaultAsync(query, cancellationToken);

        if (scheduler == null)
        {
            throw new KeyNotFoundException($"Scheduler with ID {request.Id} not found");
        }

        var needsReregistration = false;

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            scheduler.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            scheduler.Description = dto.Description;
        }

        if (dto.TimeZone != null)
        {
            // Validate timezone before updating
            if (!_schedulerService.ValidateTimeZone(dto.TimeZone))
            {
                _logger.LogError("Invalid timezone: {TimeZone}", dto.TimeZone);
                throw new ArgumentException($"Invalid timezone: {dto.TimeZone}");
            }
            scheduler.TimeZone = dto.TimeZone;
            needsReregistration = true;
        }

        if (dto.LocationType.HasValue)
        {
            scheduler.LocationType = dto.LocationType.Value;
            needsReregistration = true;
        }

        if (dto.LocationIds != null)
        {
            scheduler.LocationIdsJson = JsonConvert.SerializeObject(dto.LocationIds);
            needsReregistration = true;
        }

        if (dto.SendNotifications.HasValue)
        {
            scheduler.SendNotifications = dto.SendNotifications.Value;
        }

        if (dto.NotificationEmails != null)
        {
            scheduler.NotificationEmailsJson = JsonConvert.SerializeObject(dto.NotificationEmails);
        }

        if (dto.StartDate.HasValue)
        {
            scheduler.StartDate = dto.StartDate;
        }

        if (dto.EndDate.HasValue)
        {
            scheduler.EndDate = dto.EndDate;
        }

        // Handle frequency and configuration changes (including hierarchical settings)
        if (dto.Frequency.HasValue || dto.Configuration != null || 
            dto.GenerateHierarchicalAssessments.HasValue || dto.HierarchicalDepth.HasValue || 
            dto.MaxHierarchicalLocations.HasValue || dto.ForceRecalculate.HasValue)
        {
            // Parse existing configuration
            var existingJobConfig = string.IsNullOrEmpty(scheduler.ConfigurationJson)
                ? new JobExecutionConfigurationDto()
                : JsonConvert.DeserializeObject<JobExecutionConfigurationDto>(scheduler.ConfigurationJson) ?? new JobExecutionConfigurationDto();

            var frequency = dto.Frequency ?? scheduler.Frequency;
            var scheduleConfig = dto.Configuration ?? existingJobConfig.ScheduleConfiguration ?? new ScheduleConfigurationDto();

            // Update job execution configuration
            var jobConfig = new JobExecutionConfigurationDto
            {
                GenerateHierarchicalAssessments = dto.GenerateHierarchicalAssessments ?? existingJobConfig.GenerateHierarchicalAssessments,
                HierarchicalDepth = dto.HierarchicalDepth ?? existingJobConfig.HierarchicalDepth,
                MaxHierarchicalLocations = dto.MaxHierarchicalLocations ?? existingJobConfig.MaxHierarchicalLocations,
                ForceRecalculate = dto.ForceRecalculate ?? existingJobConfig.ForceRecalculate,
                ScheduleConfiguration = scheduleConfig
            };

            scheduler.Frequency = frequency;
            scheduler.ConfigurationJson = JsonConvert.SerializeObject(jobConfig);
            scheduler.CronExpression = _schedulerService.GenerateCronExpression(scheduleConfig, frequency);
            scheduler.NextExecutionTime = _schedulerService.GetNextExecutionTime(scheduler.CronExpression, scheduler.TimeZone);
            
            needsReregistration = true;
        }

        // Handle status changes
        if (dto.Status.HasValue && dto.Status.Value != scheduler.Status)
        {
            var oldStatus = scheduler.Status;
            scheduler.Status = dto.Status.Value;

            if (oldStatus == SchedulerStatusEnum.Active && dto.Status.Value == SchedulerStatusEnum.Paused)
            {
                await _schedulerService.PauseSchedulerAsync(scheduler.HangfireJobId);
            }
            else if (oldStatus == SchedulerStatusEnum.Paused && dto.Status.Value == SchedulerStatusEnum.Active)
            {
                await _schedulerService.ResumeSchedulerAsync(scheduler);
            }
        }
        else if (needsReregistration && scheduler.Status == SchedulerStatusEnum.Active)
        {
            // Re-register the job if configuration changed and scheduler is active
            await _schedulerService.RegisterSchedulerAsync(scheduler);
        }

        // Save changes
        _repository.Update(scheduler);
        await _repository.SaveChangesAsync(cancellationToken);

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
