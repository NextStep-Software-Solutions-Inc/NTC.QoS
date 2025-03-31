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

public class GetStudyMunicipalityByIdHandler(IStudyMunicipalityRepository studyMunicipalityRepository) : IRequestHandler<GetStudyMunicipalityByIdRequest, ApiResponse<StudyMunicipalityDto>>
{
    private readonly IStudyMunicipalityRepository _studyMunicipalityRepository = studyMunicipalityRepository;
    public async Task<ApiResponse<StudyMunicipalityDto>> Handle(GetStudyMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var studyMunicipality = await _studyMunicipalityRepository.GetByIdAsync(request.Id, cancellationToken);

        if (studyMunicipality is null)
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
            Data = new StudyMunicipalityDto
            {
                Id = studyMunicipality.Id,
                Study = new StudyDto
                {
                    Id = studyMunicipality.StudyId,
                    Name = studyMunicipality.Study?.Name ?? string.Empty,
                    Description = studyMunicipality.Study?.Description ?? string.Empty,
                    StudyRadius = studyMunicipality.Study?.StudyRadius ?? 0,
                    AreaStudy = new AreaStudyDto
                    {
                        Id = studyMunicipality.Study?.AreaStudyId ?? Guid.Empty,
                        Name = studyMunicipality.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = studyMunicipality.Study?.Description
                    }
                },
                Municipality = new MunicipalityDto
                {
                    Id = studyMunicipality.MunicipalityId,
                    Name = studyMunicipality.Municipality?.Name ?? string.Empty,
                    IsCity = studyMunicipality.Municipality?.IsCity ?? false,
                    Description = studyMunicipality.Municipality?.Description,
                    Province = new ProvinceDto
                    {
                        Id = studyMunicipality.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = studyMunicipality.Municipality?.Province?.Name ?? string.Empty,
                        Description = studyMunicipality.Municipality?.Province?.Description,
                        Region = new RegionDto
                        {
                            Id = studyMunicipality.Municipality?.Province?.RegionId ?? Guid.Empty,
                            Name = studyMunicipality.Municipality?.Province?.Region?.Name ?? string.Empty,
                            Description = studyMunicipality.Municipality?.Province?.Region?.Description
                        }
                    }
                },
            },
            Succeeded = true,
            Message = "Study Municipality retrieved successfully."
        };
    }
}
