using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Commands;

public class UpdateAreaStudyByIdHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<UpdateAreaStudyByIdRequest, ApiResponse<AreaStudyDto>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;
    public async Task<ApiResponse<AreaStudyDto>> Handle(UpdateAreaStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var areaStudyQuery = _repository.Query(a => a.Id == request.Id && !a.IsDeleted);
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
                        "Id", ["Area study with the specified Id does not exist."]
                    }
                }
            };
        }

        areaStudy.Name = request.AreaStudy.Name ?? areaStudy.Name;
        areaStudy.Description = request.AreaStudy.Description ?? areaStudy.Description;

        var existingAreaStudyQuery = _repository.Query(a => a.Name == areaStudy.Name && a.Id != request.Id && !a.IsDeleted);
        var existingAreaStudy = await _repository.FirstOrDefaultAsync(existingAreaStudyQuery, cancellationToken);

        if (existingAreaStudy is not null)
        {
            return new ApiResponse<AreaStudyDto>
            {
                Succeeded = false,
                Message = "Duplicate area study found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Area study with the same name found."]
                    }
                }
            };
        }

         _repository.Update(areaStudy);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<AreaStudyDto>
        {
            Data = new AreaStudyDto 
            { 
                Id = areaStudy.Id,
                Name = areaStudy.Name,
                Description = areaStudy.Description
            },
            Succeeded = true,
            Message = "Area study updated successfully."
        };
    }
}
