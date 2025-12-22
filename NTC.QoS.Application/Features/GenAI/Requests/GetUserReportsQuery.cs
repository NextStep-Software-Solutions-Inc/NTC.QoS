using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Query to get all reports for the current user
/// </summary>
public class GetUserReportsQuery : IRequest<ApiResponse<List<GeneratedReportSummaryDto>>>
{
    public int? Skip { get; set; }
    public int? Take { get; set; }
}
