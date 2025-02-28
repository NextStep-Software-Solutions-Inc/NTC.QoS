using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Study.Handlers.Commands;

public class DeleteStudyHandler(IRepository<Domain.Entities.Study> repository) : IRequestHandler<DeleteStudyByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Study> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var study = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (study == null)
        {
            return new ApiResponse<bool>("Study not found");
        }
        study.IsDeleted = true;
        _repository.Update(study);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>(true, true, "Study deleted successfully.");
    }
}
