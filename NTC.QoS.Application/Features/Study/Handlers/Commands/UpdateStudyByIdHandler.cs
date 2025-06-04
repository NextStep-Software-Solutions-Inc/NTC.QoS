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

        if(!String.IsNullOrEmpty(request.Study.Name)) study.Name = request.Study.Name;
        if (request.Study.AreaStudyId.HasValue)
        {
            var areaStudyQuery = _areaStudyRepository.Query(a => a.Id == request.Study.AreaStudyId.Value && !a.IsDeleted);
            var areaStudy = await _areaStudyRepository.FirstOrDefaultAsync(areaStudyQuery, cancellationToken);
            if (areaStudy is null)
            {
                return new ApiResponse<StudyDto>
                {
                    Message = "Area study not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "AreaStudyId", [$"Area study with ID [{request.Study.AreaStudyId}] does not exist."]
                        }
                    },
                    Succeeded = false
                };
            }
            study.AreaStudyId = areaStudy.Id;
        }
        if (!String.IsNullOrEmpty(request.Study.Description)) study.Description = request.Study.Description;
        if (request.Study.StudyRadius.HasValue) study.StudyRadius = request.Study.StudyRadius.Value;

        var studyExistQuery = _studyRepository.Query(s => s.Name == study.Name && s.AreaStudyId == study.AreaStudyId && !s.IsDeleted);
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
