using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Commands;

public class CreateStudyHandler(IRepository<Domain.Entities.Study> repository, IRepository<Domain.Entities.AreaStudy> areaStudyRepository) : IRequestHandler<CreateStudyRequest, ApiResponse<StudyDto>>
{
    private readonly IRepository<Domain.Entities.Study> _repository = repository;
    private readonly IRepository<Domain.Entities.AreaStudy> _areaStudyRepository = areaStudyRepository;

    public async Task<ApiResponse<StudyDto>> Handle(CreateStudyRequest request, CancellationToken cancellationToken)
    {
        var studyExist = await _repository.FindAsync(s => s.Name == request.Study.Name && !s.IsDeleted, cancellationToken);
        if (studyExist is not null)
        {
            return new ApiResponse<StudyDto>
            {
                Message = "Study already exists.",
                Succeeded = false
            };
        }

        var areaStudy = await _areaStudyRepository.GetByIdAsync(request.Study.AreaStudyId, cancellationToken);
        if (areaStudy is null)
        {
            return new ApiResponse<StudyDto>
            {
                Message = "Area study not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "AreaStudyId", [$"Area study with ID [{request.Study.AreaStudyId}] not found."]  }
                },
                Succeeded = false
            };
        }

        var study = new Domain.Entities.Study
        {
            Name = request.Study.Name,
            Description = request.Study.Description,
            AreaStudyId = areaStudy.Id,
            StudyRadius = request.Study.StudyRadius,
        };

        _repository.Add(study);
        await _repository.SaveChangesAsync(cancellationToken);

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
            Message = "Study created successfully.",
            Succeeded = true
        };
    }
}
