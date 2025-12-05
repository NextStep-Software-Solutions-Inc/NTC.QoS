using MediatR;

namespace NTC.QoS.Application.Features.Scheduler.Requests.Commands;

/// <summary>
/// Command to pause a connectivity assessment schedule
/// </summary>
public class PauseSchedulerRequest : IRequest<bool>
{
    public Guid Id { get; set; }
}
