using MediatR;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Application.Features.Scheduler.Requests.Commands;

/// <summary>
/// Command to create a new connectivity assessment schedule
/// </summary>
public class CreateSchedulerRequest : IRequest<SchedulerResponseDto>
{
    public CreateSchedulerDto Data { get; set; } = new();
}
