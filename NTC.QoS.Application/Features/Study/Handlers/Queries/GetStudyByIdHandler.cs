using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Queries;

public class GetStudyByIdHandler(IStudyRepository studyRepository) : IRequestHandler<GetStudyByIdRequest, ApiResponse<StudyDto>>
{
    private readonly IStudyRepository _studyRepository = studyRepository;

    public async Task<ApiResponse<StudyDto>> Handle(GetStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var studyDtoQuery = _studyRepository.Query(s => s.Id == request.Id)
            .Select(study => new StudyDto
            {
                Id = study.Id,
                Name = study.Name,
                Description = study.Description,
                AreaStudy = study.AreaStudy != null ? new()
                {
                    Id = study.AreaStudyId,
                    Name = study.AreaStudy.Name,
                    Description = study.AreaStudy.Description
                } : null,
                StudyRadius = study.StudyRadius
            });

        var studyDto = await _studyRepository.FirstOrDefaultAsync(studyDtoQuery, cancellationToken);
        
        if (studyDto is null)
        {
            return new ApiResponse<StudyDto>
            { 
                Message = "Study not found.",
                Errors =  new Dictionary<string, string[]>
                {
                    { 
                        "Id", [$"Study with ID [{request.Id}] does not exist."] 
                    }
                },
                Succeeded = false
            };
        }

        return new ApiResponse<StudyDto> 
        { 
            Data = studyDto, 
            Succeeded = true,
            Message = "Study retrieved successfully."
        };
    }
}
