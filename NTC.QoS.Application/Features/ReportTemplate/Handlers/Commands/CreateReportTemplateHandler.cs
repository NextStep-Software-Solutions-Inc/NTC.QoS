using MediatR;
using NTC.QoS.Application.Features.ReportTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Handlers.Commands;

public class CreateReportTemplateHandler(IRepository<Domain.Entities.ReportTemplate> repository) : IRequestHandler<CreateReportTemplateRequest, ApiResponse<ReportTemplateDto>>
{
    private readonly IRepository<Domain.Entities.ReportTemplate> _repository = repository;

    public async Task<ApiResponse<ReportTemplateDto>> Handle(CreateReportTemplateRequest request, CancellationToken cancellationToken)
    {
        var newTemplate = new Domain.Entities.ReportTemplate
        {
            Title = request.CreateTemplate.Title,
            Description = request.CreateTemplate.Description,
            Instruction = request.CreateTemplate.Instruction,
            IsActive = true
        };

        _repository.Add(newTemplate);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ReportTemplateDto>
        {
            Succeeded = true,
            Message = "Report template created successfully.",
            Data = new ReportTemplateDto
            {
                Id = newTemplate.Id,
                Title = newTemplate.Title,
                Description = newTemplate.Description,
                Instruction = newTemplate.Instruction,
                IsActive = newTemplate.IsActive,
                CreatedAt = newTemplate.CreatedAt,
                UpdatedAt = newTemplate.UpdatedAt
            }
        };
    }
}
