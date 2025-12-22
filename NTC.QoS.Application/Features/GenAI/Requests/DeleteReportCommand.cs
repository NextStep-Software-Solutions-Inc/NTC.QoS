using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Command to delete a generated report
/// </summary>
public class DeleteReportCommand(Guid reportId) : IRequest<ApiResponse<bool>>
{
    public Guid ReportId { get; set; } = reportId;
}
