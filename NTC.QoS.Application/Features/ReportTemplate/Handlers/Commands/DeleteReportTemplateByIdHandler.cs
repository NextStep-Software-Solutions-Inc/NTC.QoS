using MediatR;
using NTC.QoS.Application.Features.ReportTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.ReportTemplate.Handlers.Commands;

public class DeleteReportTemplateByIdHandler(IRepository<Domain.Entities.ReportTemplate> repository) : IRequestHandler<DeleteReportTemplateByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.ReportTemplate> _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteReportTemplateByIdRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query(t => t.Id == request.Id);
        var template = await _repository.FirstOrDefaultAsync(query, cancellationToken);

        if (template == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Report template not found.",
                Data = false
            };
        }

        _repository.Delete(template);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = "Report template deleted successfully.",
            Data = true
        };
    }
}
