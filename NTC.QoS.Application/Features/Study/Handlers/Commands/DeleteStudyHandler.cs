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
        var studyQuery = _repository.Query(s => s.Id == request.Id && !s.IsDeleted);
        var study = await _repository.FirstOrDefaultAsync(studyQuery, cancellationToken);
        if (study is null)
        {
            return new ApiResponse<bool>
            {
                Message = "Study not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", [$"Study with ID [{request.Id}] does not exist."]
                    }
                },
                Succeeded = false,
            };
        }
        _repository.Delete(study);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>(true, true, "Study deleted successfully.");
    }
}
