using MediatR;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Handlers.Queries;

public class GetStudyMunicipalityByIdHandler(IStudyMunicipalityRepository studyMunicipalityRepository) : IRequestHandler<GetStudyMunicipalityByIdRequest, ApiResponse<StudyMunicipalityDto>>
{
    private readonly IStudyMunicipalityRepository _studyMunicipalityRepository = studyMunicipalityRepository;
    
    public async Task<ApiResponse<StudyMunicipalityDto>> Handle(GetStudyMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var studyMunicipalityDtoQuery = _studyMunicipalityRepository.Query(sm => sm.Id == request.Id)
            .Select(studyMunicipality => new StudyMunicipalityDto
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
                    } : new()
                    {
                        Id = studyMunicipality.Study.AreaStudyId,
                        Name = string.Empty,
                        Description = null
                    }
                } : new()
                {
                    Id = studyMunicipality.StudyId,
                    Name = string.Empty,
                    Description = string.Empty,
                    StudyRadius = 0,
                    AreaStudy = new()
                    {
                        Id = Guid.Empty,
                        Name = string.Empty,
                        Description = null
                    }
                },
                Municipality = studyMunicipality.Municipality != null ? new()
                {
                    Id = studyMunicipality.MunicipalityId,
                    Name = studyMunicipality.Municipality.Name,
                    IsCity = studyMunicipality.Municipality.IsCity,
                    Description = studyMunicipality.Municipality.Description,
                    Province = studyMunicipality.Municipality.Province != null ? new()
                    {
                        Id = studyMunicipality.Municipality.ProvinceId,
                        Name = studyMunicipality.Municipality.Province.Name,
                        Description = studyMunicipality.Municipality.Province.Description,
                        Region = studyMunicipality.Municipality.Province.Region != null ? new()
                        {
                            Id = studyMunicipality.Municipality.Province.RegionId,
                            Name = studyMunicipality.Municipality.Province.Region.Name,
                            Description = studyMunicipality.Municipality.Province.Region.Description
                        } : new()
                        {
                            Id = studyMunicipality.Municipality.ProvinceId,
                            Name = string.Empty,
                            Description = null
                        }
                    } : new()
                    {
                        Id = studyMunicipality.MunicipalityId,
                        Name = string.Empty,
                        Description = null,
                        Region = new()
                        {
                            Id = Guid.Empty,
                            Name = string.Empty,
                            Description = null
                        }
                    }
                } : new()
                {
                    Id = studyMunicipality.MunicipalityId,
                    Name = string.Empty,
                    IsCity = false,
                    Description = null,
                    Province = new()
                    {
                        Id = Guid.Empty,
                        Name = string.Empty,
                        Description = null,
                        Region = new()
                        {
                            Id = Guid.Empty,
                            Name = string.Empty,
                            Description = null
                        }
                    }
                }
            });

        var studyMunicipalityDto = await _studyMunicipalityRepository.FirstOrDefaultAsync(studyMunicipalityDtoQuery, cancellationToken);

        if (studyMunicipalityDto is null)
        {
            return new ApiResponse<StudyMunicipalityDto>
            {
                Succeeded = false,
                Message = "Study Municipality does not exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudyMunicipality", ["Study Municipality not found."]
                    }
                }
            };
        }

        return new ApiResponse<StudyMunicipalityDto>
        {
            Data = studyMunicipalityDto,
            Succeeded = true,
            Message = "Study Municipality retrieved successfully."
        };
    }
}
