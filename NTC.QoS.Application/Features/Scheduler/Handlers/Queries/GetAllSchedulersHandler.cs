using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NTC.QoS.Application.Features.Scheduler.Requests.Queries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Application.Features.Scheduler.Handlers.Queries;

/// <summary>
/// Handler for getting all connectivity assessment schedules with pagination
/// </summary>
public class GetAllSchedulersHandler : IRequestHandler<GetAllSchedulersRequest, PaginatedResponse<SchedulerResponseDto>>
{
    private readonly IConnectivityAssessmentSchedulerRepository _repository;
    private readonly ISchedulerService _schedulerService;

    public GetAllSchedulersHandler(
        IConnectivityAssessmentSchedulerRepository repository,
        ISchedulerService schedulerService)
    {
        _repository = repository;
        _schedulerService = schedulerService;
    }

    public async Task<PaginatedResponse<SchedulerResponseDto>> Handle(GetAllSchedulersRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(s => 
                s.Name.ToLower().Contains(searchTerm) || 
                (s.Description != null && s.Description.ToLower().Contains(searchTerm)));
        }

        // Order by creation date descending
        query = query.OrderByDescending(s => s.CreatedAt);

        // Paginate first (materialize the query)
        var result = await _repository.PaginateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        // Project to DTO after materialization
        var dtoList = result.Data?.Select(s => MapToResponseDto(s, _schedulerService)).ToList() ?? new List<SchedulerResponseDto>();

        // Create new paginated response with DTOs
        var dtoResult = new PaginatedResponse<SchedulerResponseDto>(
            dtoList,
            result.PageNumber,
            result.PageSize,
            result.TotalRows
        );

        return dtoResult;
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
