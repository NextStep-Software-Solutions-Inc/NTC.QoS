using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NTC.QoS.Application.Features.Scheduler.Requests.Queries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Application.Features.Scheduler.Handlers.Queries;

/// <summary>
/// Handler for getting a specific connectivity assessment schedule by ID
/// </summary>
public class GetSchedulerByIdHandler : IRequestHandler<GetSchedulerByIdRequest, SchedulerResponseDto?>
{
    private readonly IConnectivityAssessmentSchedulerRepository _repository;
    private readonly ISchedulerService _schedulerService;

    public GetSchedulerByIdHandler(
        IConnectivityAssessmentSchedulerRepository repository,
        ISchedulerService schedulerService)
    {
        _repository = repository;
        _schedulerService = schedulerService;
    }

    public async Task<SchedulerResponseDto?> Handle(GetSchedulerByIdRequest request, CancellationToken cancellationToken)
    {
        var scheduler = await _repository.Query(s => s.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (scheduler == null)
        {
            return null;
        }

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
