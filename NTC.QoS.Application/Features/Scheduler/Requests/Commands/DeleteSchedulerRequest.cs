using MediatR;

namespace NTC.QoS.Application.Features.Scheduler.Requests.Commands;

/// <summary>
/// Command to delete a connectivity assessment schedule
/// </summary>
public class DeleteSchedulerRequest : IRequest<bool>
{
    public Guid Id { get; set; }
}
