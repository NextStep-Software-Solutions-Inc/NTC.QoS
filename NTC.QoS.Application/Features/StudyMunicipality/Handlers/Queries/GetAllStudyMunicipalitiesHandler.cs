using MediatR;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.Study;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Handlers.Queries;

public class GetAllStudyMunicipalitiesHandler(IStudyMunicipalityRepository studyMunicipalityRepository) : IRequestHandler<GetAllStudyMunicipalitiesRequest, PaginatedResponse<StudyMunicipalityDto>>
{
    private readonly IStudyMunicipalityRepository _studyMunicipalityRepository = studyMunicipalityRepository;
    public async Task<PaginatedResponse<StudyMunicipalityDto>> Handle(GetAllStudyMunicipalitiesRequest request, CancellationToken cancellationToken)
    {
        var allStudyMunicipalities = _studyMunicipalityRepository.Query(sm => !sm.IsDeleted);

        allStudyMunicipalities = allStudyMunicipalities
            .Where(sm => !request.MunicipalityId.HasValue || sm.MunicipalityId == request.MunicipalityId.Value)
            .Where(sm => !request.StudyId.HasValue || sm.StudyId == request.StudyId.Value);

        var studyMunicipalityDtos = allStudyMunicipalities.Select(studyMunicipality => new StudyMunicipalityDto
        {
            Id = studyMunicipality.Id,
            Study = new StudyDto
            {
                Id = studyMunicipality.StudyId,
                Name = studyMunicipality.Study != null ? studyMunicipality.Study.Name : string.Empty,
                Description = studyMunicipality.Study != null ? studyMunicipality.Study.Description : string.Empty,
                StudyRadius = studyMunicipality.Study != null ? studyMunicipality.Study.StudyRadius : 0,
                AreaStudy = new AreaStudyDto
                {
                    Id = studyMunicipality.Study != null && studyMunicipality.Study.AreaStudy != null ? studyMunicipality.Study.AreaStudyId : Guid.Empty,
                    Name = studyMunicipality.Study != null && studyMunicipality.Study.AreaStudy != null ? studyMunicipality.Study.AreaStudy.Name : string.Empty,
                    Description = studyMunicipality.Study != null ? studyMunicipality.Study.Description : null
                }
            },
            Municipality = new MunicipalityDto
            {
                Id = studyMunicipality.MunicipalityId,
                Name = studyMunicipality.Municipality != null ? studyMunicipality.Municipality.Name : string.Empty,
                IsCity = studyMunicipality.Municipality != null ? studyMunicipality.Municipality.IsCity : false,
                Description = studyMunicipality.Municipality != null ? studyMunicipality.Municipality.Description : null,
                Province = new ProvinceDto
                {
                    Id = studyMunicipality.Municipality != null && studyMunicipality.Municipality.Province != null ? studyMunicipality.Municipality.ProvinceId : Guid.Empty,
                    Name = studyMunicipality.Municipality != null && studyMunicipality.Municipality.Province != null ? studyMunicipality.Municipality.Province.Name : string.Empty,
                    Description = studyMunicipality.Municipality != null && studyMunicipality.Municipality.Province != null ? studyMunicipality.Municipality.Province.Description : null,
                    Region = new RegionDto
                    {
                        Id = studyMunicipality.Municipality != null && studyMunicipality.Municipality.Province != null && studyMunicipality.Municipality.Province.Region != null ? studyMunicipality.Municipality.Province.RegionId : Guid.Empty,
                        Name = studyMunicipality.Municipality != null && studyMunicipality.Municipality.Province != null && studyMunicipality.Municipality.Province.Region != null ? studyMunicipality.Municipality.Province.Region.Name : string.Empty,
                        Description = studyMunicipality.Municipality != null && studyMunicipality.Municipality.Province != null && studyMunicipality.Municipality.Province.Region != null ? studyMunicipality.Municipality.Province.Region.Description : null
                    }
                }
            },
        });

        return await _studyMunicipalityRepository.PaginateAsync(studyMunicipalityDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
