using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Queries;

public class GetAllStudiesHandler(IStudyRepository studyRepository) : IRequestHandler<GetAllStudiesRequest, PaginatedResponse<StudyDto>>
{
    private readonly IStudyRepository _studyRepository = studyRepository;
    public async Task<PaginatedResponse<StudyDto>> Handle(GetAllStudiesRequest request, CancellationToken cancellationToken)
    {
        var studies = await _studyRepository.ListAsync(s => !s.IsDeleted, cancellationToken);

        if (request.AreaStudyId.HasValue)
        {
            studies = [.. studies.Where(s => s.AreaStudyId == request.AreaStudyId.Value)];
        }

        var allStudies = studies.Select(study => new StudyDto
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
        }).ToList();

        if (allStudies.Count is 0)
        {
            return new PaginatedResponse<StudyDto>(new List<StudyDto>(), request.PageNumber, request.PageSize)
            {
                Data = allStudies,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<StudyDto>(allStudies, request.PageNumber, request.PageSize)
        {

            Data = allStudies,
            Message = "Studies retrieved successfully.",
            Succeeded = true
        };
    }
}
