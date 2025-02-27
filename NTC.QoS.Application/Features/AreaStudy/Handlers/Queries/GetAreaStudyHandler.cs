using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Queries;

public class GetAreaStudyHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<GetAreaStudyByIdRequest, ApiResponse<Domain.Entities.AreaStudy>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.AreaStudy>> Handle(GetAreaStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var areaStudy = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (areaStudy == null)
        {
            return new ApiResponse<Domain.Entities.AreaStudy>("Area Study not found");
        }
        return new ApiResponse<Domain.Entities.AreaStudy>(areaStudy);
    }
}
