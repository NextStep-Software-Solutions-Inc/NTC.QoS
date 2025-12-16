using MediatR;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Application.Features.Scheduler.Requests.Commands;

/// <summary>
/// Command to update an existing connectivity assessment schedule
/// </summary>
public class UpdateSchedulerRequest : IRequest<SchedulerResponseDto>
{
    public Guid Id { get; set; }
    public UpdateSchedulerDto Data { get; set; } = new();
}
