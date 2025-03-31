using MediatR;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.Study;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Handlers.Commands;

public class CreateStudyMunicipalityHandler(IStudyMunicipalityRepository repository) : IRequestHandler<CreateStudyMunicipalityRequest, ApiResponse<StudyMunicipalityDto>>
{
    private readonly IStudyMunicipalityRepository _repository = repository;

    public async Task<ApiResponse<StudyMunicipalityDto>> Handle(CreateStudyMunicipalityRequest request, CancellationToken cancellationToken)
    {
        var studyMunicipality = await _repository.FindAsync(sp => sp.StudyId == request.StudyMunicipality.StudyId
                                                                    && sp.MunicipalityId == request.StudyMunicipality.MunicipalityId,
                                                                    cancellationToken);

        if (studyMunicipality is not null && studyMunicipality.IsDeleted.Equals(true))
        {
            studyMunicipality.IsDeleted = false;
            _repository.Update(studyMunicipality);
            await _repository.SaveChangesAsync(cancellationToken);

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
                        AreaStudy = new AreaStudyStudyDto
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
                Message = "Study reactivated successfully.",
                Succeeded = true
            };
        }

        if (studyMunicipality is not null && studyMunicipality.IsDeleted.Equals(false))
        {
            return new ApiResponse<StudyMunicipalityDto>
            {
                Message = "Study Municipality already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Study Municipality already exists."]
                    }
                },
            };
        }

        var newStudyMunicipality = new Domain.Aggregates.StudyMunicipality
        {
            StudyId = request.StudyMunicipality.StudyId,
            MunicipalityId = request.StudyMunicipality.MunicipalityId,
        };

        _repository.Add(newStudyMunicipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<StudyMunicipalityDto>
        {
            Data = new StudyMunicipalityDto
            {
                Id = newStudyMunicipality.Id,
                Study = new StudyDto
                {
                    Id = newStudyMunicipality.StudyId,
                    Name = newStudyMunicipality.Study?.Name ?? string.Empty,
                    Description = newStudyMunicipality.Study?.Description ?? string.Empty,
                    StudyRadius = newStudyMunicipality.Study?.StudyRadius ?? 0,
                    AreaStudy = new AreaStudyStudyDto
                    {
                        Id = newStudyMunicipality.Study?.AreaStudyId ?? Guid.Empty,
                        Name = newStudyMunicipality.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = newStudyMunicipality.Study?.Description
                    }
                },
                Municipality = new MunicipalityDto
                {
                    Id = newStudyMunicipality.MunicipalityId,
                    Name = newStudyMunicipality.Municipality?.Name ?? string.Empty,
                    IsCity = newStudyMunicipality.Municipality?.IsCity ?? false,
                    Description = newStudyMunicipality.Municipality?.Description,
                    Province = new ProvinceDto
                    {
                        Id = newStudyMunicipality.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = newStudyMunicipality.Municipality?.Province?.Name ?? string.Empty,
                        Description = newStudyMunicipality.Municipality?.Province?.Description,
                        Region = new RegionDto
                        {
                            Id = newStudyMunicipality.Municipality?.Province?.RegionId ?? Guid.Empty,
                            Name = newStudyMunicipality.Municipality?.Province?.Region?.Name ?? string.Empty,
                            Description = newStudyMunicipality.Municipality?.Province?.Region?.Description
                        }
                    }
                },
            },
            Succeeded = true,
            Message = "Study Municipality created successfully.",
        };
    }
}
