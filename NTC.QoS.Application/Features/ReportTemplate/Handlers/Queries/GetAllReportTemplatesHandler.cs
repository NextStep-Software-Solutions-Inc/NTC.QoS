using MediatR;
using NTC.QoS.Application.Features.ReportTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Handlers.Queries;

public class GetAllReportTemplatesHandler(IRepository<Domain.Entities.ReportTemplate> repository) : IRequestHandler<GetAllReportTemplatesRequest, ApiResponse<List<ReportTemplateListDto>>>
{
    private readonly IRepository<Domain.Entities.ReportTemplate> _repository = repository;

    public async Task<ApiResponse<List<ReportTemplateListDto>>> Handle(GetAllReportTemplatesRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query(t => t.IsActive)
            .Select(t => new ReportTemplateListDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description
            });

        var templates = await _repository.ToListAsync(query, cancellationToken);

        return new ApiResponse<List<ReportTemplateListDto>>
        {
            Succeeded = true,
            Data = templates
        };
    }
}
