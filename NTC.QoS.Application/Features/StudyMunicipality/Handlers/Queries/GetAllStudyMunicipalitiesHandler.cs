using MediatR;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Handlers.Queries;

public class GetAllStudyMunicipalitiesHandler(IStudyMunicipalityRepository studyMunicipalityRepository) : IRequestHandler<GetAllStudyMunicipalitiesRequest, PaginatedResponse<StudyMunicipalityDto>>
{
    private readonly IStudyMunicipalityRepository _studyMunicipalityRepository = studyMunicipalityRepository;

    public async Task<PaginatedResponse<StudyMunicipalityDto>> Handle(GetAllStudyMunicipalitiesRequest request, CancellationToken cancellationToken)
    {
        var allStudyMunicipalities = _studyMunicipalityRepository.Query();

        allStudyMunicipalities = allStudyMunicipalities
            .Where(sm => !request.MunicipalityId.HasValue || sm.MunicipalityId == request.MunicipalityId.Value)
            .Where(sm => !request.StudyId.HasValue || sm.StudyId == request.StudyId.Value);

        var studyMunicipalityDtos = allStudyMunicipalities.Select(studyMunicipality => new StudyMunicipalityDto
        {
            Id = studyMunicipality.Id,
            Study = studyMunicipality.Study != null ? new()
            {
                Id = studyMunicipality.StudyId,
                Name = studyMunicipality.Study.Name,
                Description = studyMunicipality.Study.Description,
                StudyRadius = studyMunicipality.Study.StudyRadius,
                AreaStudy = studyMunicipality.Study.AreaStudy != null ? new()
                {
                    Id = studyMunicipality.Study.AreaStudyId,
                    Name = studyMunicipality.Study.AreaStudy.Name,
                    Description = studyMunicipality.Study.AreaStudy.Description
                } : null
            } : null,
            Municipality = studyMunicipality.Municipality != null ? new()
            {
                Id = studyMunicipality.MunicipalityId,
                Name = studyMunicipality.Municipality.Name
            } : null
        });

        return await _studyMunicipalityRepository.PaginateAsync(studyMunicipalityDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
