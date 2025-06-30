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
        var studyMunicipalityQuery = _studyMunicipalityRepository.Query(sm => sm.Id == request.Id);
        var studyMunicipality = await _studyMunicipalityRepository.FirstOrDefaultAsync(studyMunicipalityQuery, cancellationToken);

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
                Study = new()
                {
                    Id = studyMunicipality.StudyId,
                    Name = studyMunicipality.Study?.Name ?? string.Empty,
                    Description = studyMunicipality.Study?.Description ?? string.Empty,
                    StudyRadius = studyMunicipality.Study?.StudyRadius ?? 0,
                    AreaStudy = new()
                    {
                        Id = studyMunicipality.Study?.AreaStudyId ?? Guid.Empty,
                        Name = studyMunicipality.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = studyMunicipality.Study?.Description
                    }
                },
                Municipality = new()
                {
                    Id = studyMunicipality.MunicipalityId,
                    Name = studyMunicipality.Municipality?.Name ?? string.Empty,
                    IsCity = studyMunicipality.Municipality?.IsCity ?? false,
                    Description = studyMunicipality.Municipality?.Description,
                    Province = new()
                    {
                        Id = studyMunicipality.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = studyMunicipality.Municipality?.Province?.Name ?? string.Empty,
                        Description = studyMunicipality.Municipality?.Province?.Description,
                        Region = new()
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
