using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Queries;

public class GetAllStudiesHandler(IStudyRepository studyRepository) : IRequestHandler<GetAllStudiesRequest, PaginatedResponse<StudyDto>>
{
    private readonly IStudyRepository _studyRepository = studyRepository;
    public async Task<PaginatedResponse<StudyDto>> Handle(GetAllStudiesRequest request, CancellationToken cancellationToken)
    {
        var studies = await _studyRepository.ListAsync(s => !s.IsDeleted, cancellationToken);

        var allStudies = studies.Select(study => new StudyDto
        {
            Id = study.Id,
            Name = study.Name,
            Description = study.Description,
            AreaStudyId = study.AreaStudyId,
            AreaStudy = study.AreaStudy != null
                ? new AreaStudyDto
                {
                    Name = study.AreaStudy.Name,
                    Description = study.AreaStudy.Description ?? ""
                }
                : null!, 
            StudyRadius = study.StudyRadius,
            IsDeleted = study.IsDeleted
        }).ToList();

        return new PaginatedResponse<StudyDto>(allStudies, request.PageNumber, request.PageSize);
    }

}
