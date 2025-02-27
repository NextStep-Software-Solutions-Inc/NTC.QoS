using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Commands;

public class UpdateAreaStudyByIdHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<UpdateAreaStudyByIdRequest, ApiResponse<Domain.Entities.AreaStudy>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.AreaStudy>> Handle(UpdateAreaStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var areaStudy = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (areaStudy == null)
        {
            return new ApiResponse<Domain.Entities.AreaStudy>("Area Study not found");
        }
        areaStudy.Name = request.UpdateAreaStudyDto.Name;
        areaStudy.Description = request.UpdateAreaStudyDto.Description;
         _repository.Update(areaStudy);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<Domain.Entities.AreaStudy>(areaStudy);
    }
}
