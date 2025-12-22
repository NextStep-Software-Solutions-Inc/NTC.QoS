using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Requests;

public class GetAllReportTemplatesRequest : IRequest<ApiResponse<List<ReportTemplateListDto>>>
{
}
