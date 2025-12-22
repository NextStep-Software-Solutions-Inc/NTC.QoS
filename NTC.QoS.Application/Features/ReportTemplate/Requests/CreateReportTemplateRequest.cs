using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Requests;

public class CreateReportTemplateRequest(CreateTemplateDto createTemplate) : IRequest<ApiResponse<ReportTemplateDto>>
{
    public CreateTemplateDto CreateTemplate { get; set; } = createTemplate;
}
