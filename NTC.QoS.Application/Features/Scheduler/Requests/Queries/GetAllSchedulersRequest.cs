using MediatR;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Application.Features.Scheduler.Requests.Queries;

/// <summary>
/// Query to get all connectivity assessment schedules with pagination
/// </summary>
public class GetAllSchedulersRequest : IRequest<PaginatedResponse<SchedulerResponseDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
}
