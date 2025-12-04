using MediatR;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Application.Features.Scheduler.Requests.Queries;

/// <summary>
/// Query to get a specific connectivity assessment schedule by ID
/// </summary>
public class GetSchedulerByIdRequest : IRequest<SchedulerResponseDto?>
{
    public Guid Id { get; set; }
}
