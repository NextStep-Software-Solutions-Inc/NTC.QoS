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
        var study = await _studyRepository.GetByIdAsync(request.Id, cancellationToken);
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
            var areaStudy = await _areaStudyRepository.GetByIdAsync(request.Study.AreaStudyId.Value, cancellationToken);
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

        var studyExist = await _studyRepository.FindAsync(s => s.Name == study.Name && s.AreaStudyId == study.AreaStudyId && !s.IsDeleted, cancellationToken);
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
