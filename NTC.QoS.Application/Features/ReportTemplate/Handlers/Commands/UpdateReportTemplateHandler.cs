using MediatR;
using NTC.QoS.Application.Features.ReportTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Handlers.Commands;

public class UpdateReportTemplateHandler(IRepository<Domain.Entities.ReportTemplate> repository) : IRequestHandler<UpdateReportTemplateRequest, ApiResponse<ReportTemplateDto>>
{
    private readonly IRepository<Domain.Entities.ReportTemplate> _repository = repository;

    public async Task<ApiResponse<ReportTemplateDto>> Handle(UpdateReportTemplateRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query(t => t.Id == request.Id);
        var template = await _repository.FirstOrDefaultAsync(query, cancellationToken);

        if (template == null)
        {
            return new ApiResponse<ReportTemplateDto>
            {
                Succeeded = false,
                Message = "Report template not found."
            };
        }

        if (request.UpdateTemplate.Title != null)
        {
            template.Title = request.UpdateTemplate.Title;
        }

        if (request.UpdateTemplate.Description != null)
        {
            template.Description = request.UpdateTemplate.Description;
        }

        if (request.UpdateTemplate.Instruction != null)
        {
            template.Instruction = request.UpdateTemplate.Instruction;
        }

        if (request.UpdateTemplate.IsActive.HasValue)
        {
            template.IsActive = request.UpdateTemplate.IsActive.Value;
        }

        _repository.Update(template);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ReportTemplateDto>
        {
            Succeeded = true,
            Message = "Report template updated successfully.",
            Data = new ReportTemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Description = template.Description,
                Instruction = template.Instruction,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            }
        };
    }
}
