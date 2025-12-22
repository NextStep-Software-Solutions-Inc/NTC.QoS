using MediatR;
using Microsoft.AspNetCore.Http;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Request to generate a report using AI with streaming response
/// </summary>
public class GenerateReportRequest(GenerateReportRequestDto generateReportRequestDto, HttpResponse httpResponse) : IRequest<Unit>
{
    public GenerateReportRequestDto Request { get; set; } = generateReportRequestDto;
    public HttpResponse HttpResponse { get; set; } = httpResponse;
}
