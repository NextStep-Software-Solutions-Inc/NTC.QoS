using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Queries;

public class GetAreaStudyHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<GetAreaStudyByIdRequest, ApiResponse<AreaStudyDto>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;
    public async Task<ApiResponse<AreaStudyDto>> Handle(GetAreaStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var areaStudyQuery = _repository.Query(c => c.Id.Equals(request.Id));
        var areaStudy = await _repository.FirstOrDefaultAsync(areaStudyQuery, cancellationToken);

        if (areaStudy is null)
        {
            return new ApiResponse<AreaStudyDto>
            {
                Succeeded = false,
                Message = "Area study not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["Area study does not exist."]
                    }
                }
            };
        }

        return new ApiResponse<AreaStudyDto>
        {
            Data = new AreaStudyDto 
            { 
                Id = areaStudy.Id,
                Name = areaStudy.Name,
                Description = areaStudy.Description
            },
            Message = "Area study retrieved successfully.",
            Succeeded = true
        };
    }
}
