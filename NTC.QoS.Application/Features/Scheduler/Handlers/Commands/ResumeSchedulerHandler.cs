using MediatR;
using NTC.QoS.Application.Features.Scheduler.Requests.Commands;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Scheduler.Handlers.Commands;

/// <summary>
/// Handler for resuming a paused connectivity assessment schedule
/// </summary>
public class ResumeSchedulerHandler : IRequestHandler<ResumeSchedulerRequest, bool>
{
    private readonly IConnectivityAssessmentSchedulerRepository _repository;
    private readonly ISchedulerService _schedulerService;

    public ResumeSchedulerHandler(
        IConnectivityAssessmentSchedulerRepository repository,
        ISchedulerService schedulerService)
    {
        _repository = repository;
        _schedulerService = schedulerService;
    }

    public async Task<bool> Handle(ResumeSchedulerRequest request, CancellationToken cancellationToken)
    {
        // Get existing scheduler
        var query = _repository.Query(s => s.Id == request.Id);
        var scheduler = await _repository.FirstOrDefaultAsync(query, cancellationToken);

        if (scheduler == null)
        {
            return false;
        }

        if (scheduler.Status == SchedulerStatusEnum.Active)
        {
            return true; // Already active
        }

        // Resume in Hangfire
        await _schedulerService.ResumeSchedulerAsync(scheduler);

        // Update status
        scheduler.Status = SchedulerStatusEnum.Active;
        scheduler.NextExecutionTime = _schedulerService.GetNextExecutionTime(scheduler.CronExpression, scheduler.TimeZone);
        
        _repository.Update(scheduler);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
