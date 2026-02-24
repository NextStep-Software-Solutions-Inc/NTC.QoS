using MediatR;
using NTC.QoS.Application.Features.ReportTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Handlers.Queries;

public class GetReportTemplateByIdHandler(IRepository<Domain.Entities.ReportTemplate> repository) : IRequestHandler<GetReportTemplateByIdRequest, ApiResponse<ReportTemplateDto>>
{
    private readonly IRepository<Domain.Entities.ReportTemplate> _repository = repository;

    public async Task<ApiResponse<ReportTemplateDto>> Handle(GetReportTemplateByIdRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query(t => t.Id == request.Id)
            .Select(t => new ReportTemplateDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Instruction = t.Instruction,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            });

        var template = await _repository.FirstOrDefaultAsync(query, cancellationToken);

        if (template == null)
        {
            return new ApiResponse<ReportTemplateDto>
            {
                Succeeded = false,
                Message = "Report template not found."
            };
        }

        return new ApiResponse<ReportTemplateDto>
        {
            Succeeded = true,
            Data = template
        };
    }
}
