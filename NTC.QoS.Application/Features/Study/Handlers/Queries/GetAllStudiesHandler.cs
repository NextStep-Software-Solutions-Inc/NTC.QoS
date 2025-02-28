using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Study.Handlers.Queries;

public class GetAllStudiesHandler(IStudyRepository studyRepository) : IRequestHandler<GetAllStudiesRequest, PaginatedResponse<Domain.Entities.Study>>
{
    private readonly IStudyRepository _studyRepository = studyRepository;
    public async Task<PaginatedResponse<Domain.Entities.Study>> Handle(GetAllStudiesRequest request, CancellationToken cancellationToken)
    {
        var studies = await _studyRepository.ListAsync(s => !s.IsDeleted, cancellationToken);

        var allStudies = studies.Select(study => new Domain.Entities.Study
        {
            Id = study.Id,
            Name = study.Name,
            Description = study.Description,
            AreaStudyId = study.AreaStudyId,
            AreaStudy = study.AreaStudy != null
                ? new ()
                {
                    Name = study.AreaStudy.Name,
                    Description = study.AreaStudy.Description
                }
                : null, 
            StudyRadius = study.StudyRadius,
            IsDeleted = study.IsDeleted
        }).ToList();

        return new PaginatedResponse<Domain.Entities.Study>(allStudies, request.PageNumber, request.PageSize);
    }

}
