using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
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
                Error = [$"Study with ID [{request.Id}] does not exist."],
                Succeeded = false,
            };
        }

        if (request.Study.AreaStudyId.HasValue)
        {
            var areaStudy = await _areaStudyRepository.GetByIdAsync(request.Study.AreaStudyId.Value, cancellationToken);
            if (areaStudy is null)
            {
                return new ApiResponse<StudyDto>
                {
                    Message = "Area study not found.",
                    Error = [$"Area study with ID [{request.Study.AreaStudyId}] does not exist."],
                    Succeeded = false
                };
            }
            study.AreaStudyId = request.Study.AreaStudyId.Value;
        }

        if (request.Study.Name is not null)
        {
            study.Name = request.Study.Name;
        }

        if (request.Study.Description is not null)
        {
            study.Description = request.Study.Description;
        }

        if (request.Study.StudyRadius.HasValue)
        {
            study.StudyRadius = request.Study.StudyRadius.Value;
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
                AreaStudy = new AreaStudyStudyDto
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
