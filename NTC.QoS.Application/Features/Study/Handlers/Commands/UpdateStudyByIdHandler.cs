using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Commands;

public class UpdateStudyByIdHandler(IStudyRepository studyRepository, IRepository<Domain.Entities.AreaStudy> areaStudyRepository) : IRequestHandler<UpdateStudyRequest, ApiResponse<StudyDto>>
{
    private readonly IStudyRepository _studyRepository = studyRepository;
    private readonly IRepository<Domain.Entities.AreaStudy> _areaStudyRepository = areaStudyRepository;

    public async Task<ApiResponse<StudyDto>> Handle(UpdateStudyRequest request, CancellationToken cancellationToken)
    {
        var studyQuery = _studyRepository.Query(s => s.Id == request.Id && !s.IsDeleted);
        var study = await _studyRepository.FirstOrDefaultAsync(studyQuery, cancellationToken);
        if (study is null)
        {
            return new ApiResponse<StudyDto>
            {
                Message = "Study not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", [$"Study with ID [{request.Id}] does not exist."]
                    }
                },
                Succeeded = false
            };
        }

        study.Name = request.Study.Name ?? study.Name;
        study.Description = request.Study.Description ?? study.Description;
        study.StudyRadius = request.Study.StudyRadius ?? study.StudyRadius;

        var studyExistQuery = _studyRepository.Query(s => s.Name == study.Name && s.AreaStudyId == study.AreaStudyId && s.Description == study.Description && !s.IsDeleted);
        var studyExist = await _studyRepository.FirstOrDefaultAsync(studyExistQuery, cancellationToken);
        if (studyExist is not null)
        {
            return new ApiResponse<StudyDto>
            {
                Message = "Duplicate study found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Study with the same name and area study already exists."]
                    }
                },
                Succeeded = false
            };
        }

        _studyRepository.Update(study);
        await _studyRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<StudyDto>
        {
            Data = new StudyDto
            {
                Id = study.Id,
                Name = study.Name,
                Description = study.Description,
                AreaStudy = new AreaStudyDto
                {
                    Id = study.AreaStudyId,
                    Name = study.AreaStudy?.Name ?? string.Empty,
                    Description = study.AreaStudy?.Description
                },
                StudyRadius = study.StudyRadius
            },
            Succeeded = true,
            Message = "Study updated successfully."
        };
    }
}
