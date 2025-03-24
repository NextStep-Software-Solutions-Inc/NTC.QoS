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
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", [$"Study with ID [{request.Id}] does not exist."] }
                },
                Succeeded = false,
            };
        }

        if (request.Study.AreaStudyId.HasValue)
        {
            var areaStudy = await _areaStudyRepository.FindAsync(a => a.Id == request.Study.AreaStudyId && a.IsDeleted.Equals(false), cancellationToken);
            if (areaStudy is null)
            {
                return new ApiResponse<StudyDto>
                {
                    Message = "Area study not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        {"AreaSt", [$"Area study with ID [{request.Study.AreaStudyId}] not found."] }
                    },
                    Succeeded = false
                };
            }
        }

        study.Name = request.Study.Name ?? study.Name;
        study.AreaStudyId = request.Study.AreaStudyId ?? study.AreaStudyId;
        study.Description = request.Study.Description ?? study.Description;
        study.StudyRadius = request.Study.StudyRadius ?? study.StudyRadius;

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
