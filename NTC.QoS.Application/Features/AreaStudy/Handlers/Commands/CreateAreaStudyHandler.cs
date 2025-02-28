using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Commands;

public class CreateAreaStudyHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<CreateAreaStudyRequest, ApiResponse<Domain.Entities.AreaStudy>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.AreaStudy>> Handle(CreateAreaStudyRequest request, CancellationToken cancellationToken)
    {
        var areaStudyExist = await _repository.FindAsync(a => a.Name == request.AreaStudy.Name, cancellationToken);
        if (areaStudyExist != null) 
        {
            return new ApiResponse<Domain.Entities.AreaStudy>
            {
                Succeeded = false,
                Message = "Area Study already exists"
            };
        }
        var areaStudy = new Domain.Entities.AreaStudy
        {
            Name = request.AreaStudy.Name,
            Description = request.AreaStudy.Description
        };
         _repository.Add(areaStudy);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<Domain.Entities.AreaStudy>
        {
            Succeeded = true,
            Data = areaStudy,
            Message = "Area Study created successfully"
        };
    }
}
