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
        var study = await _studyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (study is null)
        {
            return new ApiResponse<StudyDto>
            { 
                Message = "Study not found.",
                Error = [$"Study with ID [{request.Id}] does not exist."],
                Succeeded = false
            };
        }

        var studyDto = new StudyDto
        {
            Id = study.Id,
            Name = study.Name,
            Description = study.Description,
            AreaStudy = new AreaStudyStudyDto
            {
                Id = study.AreaStudyId,
                Name = study.AreaStudy?.Name ?? string.Empty,
                Description = study.AreaStudy?.Description
            },
            StudyRadius = study.StudyRadius
        };

        return new ApiResponse<StudyDto> 
        { 
            Data = studyDto, 
            Succeeded = true,
            Message = "Study retrieved successfully."
        };
    }
}
