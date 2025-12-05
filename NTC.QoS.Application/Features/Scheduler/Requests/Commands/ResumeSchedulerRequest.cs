using MediatR;

namespace NTC.QoS.Application.Features.Scheduler.Requests.Commands;

/// <summary>
/// Command to resume a paused connectivity assessment schedule
/// </summary>
public class ResumeSchedulerRequest : IRequest<bool>
{
    public Guid Id { get; set; }
}
