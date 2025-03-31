using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Commands;

public class DeleteAreaStudyByIdHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<DeleteAreaStudyByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteAreaStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var areaStudy = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (areaStudy is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Area Study not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", [$"Area Study with ID [{request.Id}] does not exist."]
                    }
                }
            };
        }
        _repository.Delete(areaStudy);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>(true, true, "Area study deleted successfully.");
    }
}
