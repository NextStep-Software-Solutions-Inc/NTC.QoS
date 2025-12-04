using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Scheduler.Requests.Commands;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Scheduler.Handlers.Commands;

/// <summary>
/// Handler for deleting a connectivity assessment schedule
/// </summary>
public class DeleteSchedulerHandler : IRequestHandler<DeleteSchedulerRequest, bool>
{
    private readonly IConnectivityAssessmentSchedulerRepository _repository;
    private readonly ISchedulerService _schedulerService;

    public DeleteSchedulerHandler(
        IConnectivityAssessmentSchedulerRepository repository,
        ISchedulerService schedulerService)
    {
        _repository = repository;
        _schedulerService = schedulerService;
    }

    public async Task<bool> Handle(DeleteSchedulerRequest request, CancellationToken cancellationToken)
    {
        // Get existing scheduler
        var scheduler = await _repository.Query(s => s.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (scheduler == null)
        {
            return false;
        }

        // Remove from Hangfire
        await _schedulerService.UnregisterSchedulerAsync(scheduler.HangfireJobId);

        // Soft delete from database
        _repository.Delete(scheduler);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
