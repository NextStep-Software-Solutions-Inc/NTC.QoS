using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.ReportTemplate.Requests;

public class DeleteReportTemplateByIdRequest(Guid id) : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; } = id;
}
