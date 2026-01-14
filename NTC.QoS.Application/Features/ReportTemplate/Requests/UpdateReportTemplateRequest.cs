using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Requests;

public class UpdateReportTemplateRequest(Guid id, UpdateTemplateDto updateTemplate) : IRequest<ApiResponse<ReportTemplateDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateTemplateDto UpdateTemplate { get; set; } = updateTemplate;
}
