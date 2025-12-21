using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Request to generate a report using AI
/// </summary>
public class GenerateReportRequest(GenerateReportRequestDto generateReportRequestDto) : IRequest<ApiResponse<GenerateReportResponseDto>>
{
    public GenerateReportRequestDto Request { get; set; } = generateReportRequestDto;
}
