using MediatR;
using NTC.QoS.Application.Features.ReportTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;

namespace NTC.QoS.Application.Features.ReportTemplate.Handlers.Queries;

public class GetAllReportTemplatesHandler(IRepository<Domain.Entities.ReportTemplate> repository) : IRequestHandler<GetAllReportTemplatesRequest, PaginatedResponse<ReportTemplateListDto>>
{
    private readonly IRepository<Domain.Entities.ReportTemplate> _repository = repository;

    public async Task<PaginatedResponse<ReportTemplateListDto>> Handle(GetAllReportTemplatesRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query(t => t.IsActive);

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(t => 
                t.Title.ToLower().Contains(searchTerm) || 
                (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
        }

        // Order by creation date descending
        query = query.OrderByDescending(t => t.CreatedAt);

        // Project to DTO
        var dtoQuery = query.Select(t => new ReportTemplateListDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description
        });

        // Paginate
        var result = await _repository.PaginateAsync(dtoQuery, request.PageNumber, request.PageSize, cancellationToken);

        return result;
    }
}
