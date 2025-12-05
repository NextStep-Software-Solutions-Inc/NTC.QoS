using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Scheduler.Requests.Commands;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Scheduler.Handlers.Commands;

/// <summary>
/// Handler for pausing a connectivity assessment schedule
/// </summary>
public class PauseSchedulerHandler : IRequestHandler<PauseSchedulerRequest, bool>
{
    private readonly IConnectivityAssessmentSchedulerRepository _repository;
    private readonly ISchedulerService _schedulerService;

    public PauseSchedulerHandler(
        IConnectivityAssessmentSchedulerRepository repository,
        ISchedulerService schedulerService)
    {
        _repository = repository;
        _schedulerService = schedulerService;
    }

    public async Task<bool> Handle(PauseSchedulerRequest request, CancellationToken cancellationToken)
    {
        // Get existing scheduler
        var scheduler = await _repository.Query(s => s.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (scheduler == null)
        {
            return false;
        }

        if (scheduler.Status == SchedulerStatusEnum.Paused)
        {
            return true; // Already paused
        }

        // Pause in Hangfire
        await _schedulerService.PauseSchedulerAsync(scheduler.HangfireJobId);

        // Update status
        scheduler.Status = SchedulerStatusEnum.Paused;
        _repository.Update(scheduler);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
