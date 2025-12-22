using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Query to get all reports for the current user with pagination
/// </summary>
public class GetUserReportsQuery : IRequest<PaginatedResponse<GeneratedReportSummaryDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
