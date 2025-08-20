using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Queries;

public class GetAllStudiesHandler(IStudyRepository studyRepository) : IRequestHandler<GetAllStudiesRequest, PaginatedResponse<StudyDto>>
{
    private readonly IStudyRepository _studyRepository = studyRepository;
    public async Task<PaginatedResponse<StudyDto>> Handle(GetAllStudiesRequest request, CancellationToken cancellationToken)
    {
        var studies = _studyRepository.Query();

        var allStudies = studies.Select(study => new StudyDto
        {
            Id = study.Id,
            Name = study.Name,
            Description = study.Description,
            AreaStudy = new()
            {
                Id = study.AreaStudyId,
                Name = study.AreaStudy != null ? study.AreaStudy.Name : string.Empty,
                Description = study.AreaStudy != null ? study.AreaStudy.Description : null
            },
            StudyRadius = study.StudyRadius
        });

        allStudies = allStudies.Where(s => !request.AreaStudyId.HasValue || s.AreaStudy!.Id == request.AreaStudyId.Value);

        return await _studyRepository.PaginateAsync(allStudies, request.PageNumber, request.PageSize, cancellationToken);
    }
}
