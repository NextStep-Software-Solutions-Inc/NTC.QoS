using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Query to get a specific report by ID
/// </summary>
public class GetReportByIdQuery(Guid reportId) : IRequest<ApiResponse<GeneratedReportDto>>
{
    public Guid ReportId { get; set; } = reportId;
}
