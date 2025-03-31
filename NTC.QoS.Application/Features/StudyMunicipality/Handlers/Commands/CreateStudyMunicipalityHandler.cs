using System.Diagnostics.Eventing.Reader;
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

namespace NTC.QoS.Application.Features.StudyMunicipality.Handlers.Commands;

public class CreateStudyMunicipalityHandler(IStudyMunicipalityRepository repository, IStudyRepository studyRepository, IMunicipalityRepository municipalityRepository) : IRequestHandler<CreateStudyMunicipalityRequest, ApiResponse<StudyMunicipalityDto>>
{
    private readonly IStudyMunicipalityRepository _repository = repository;
    private readonly IStudyRepository _studyRepository = studyRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<ApiResponse<StudyMunicipalityDto>> Handle(CreateStudyMunicipalityRequest request, CancellationToken cancellationToken)
    {
        var studyMunicipality = await _repository.FindAsync(sn => sn.StudyId == request.StudyMunicipality.StudyId && sn.MunicipalityId == request.StudyMunicipality.MunicipalityId, cancellationToken);

        if (studyMunicipality is not null)
        {
            return new ApiResponse<StudyMunicipalityDto>
            {
                Message = "Study municipality already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Study municipality with the same study and municipality already exists."]
                    }
                }
            };
        }

        var study = await _studyRepository.GetByIdAsync(request.StudyMunicipality.StudyId, cancellationToken);

        if (study is null)
        {
            return new ApiResponse<StudyMunicipalityDto>
            {
                Message = "Study not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudy", ["Study not found."]
                    }
                }
            };
        }

        var municipality = await _municipalityRepository.GetByIdAsync(request.StudyMunicipality.MunicipalityId, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<StudyMunicipalityDto>
            {
                Message = "Municipality not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullMunicipality", ["Municipality not found."]
                    }
                }
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
                    Id = study.Id,
                    Name = study.Name,
                    Description = study.Description,
                    StudyRadius = study.StudyRadius,
                    AreaStudy = new AreaStudyDto
                    {
                        Id = study.AreaStudyId,
                        Name = study.AreaStudy?.Name ?? string.Empty,
                        Description = study.Description
                    }
                },
                Municipality = new MunicipalityDto
                {
                    Id = municipality.Id,
                    Name = municipality.Name,
                    IsCity = municipality.IsCity,
                    Description = municipality.Description,
                    Province = new ProvinceDto
                    {
                        Id = municipality.ProvinceId,
                        Name = municipality.Province?.Name ?? string.Empty,
                        Description = municipality.Province?.Description,
                        Region = new RegionDto
                        {
                            Id = municipality.Province?.RegionId ?? Guid.Empty,
                            Name = municipality.Province?.Region?.Name ?? string.Empty,
                            Description = municipality.Province?.Region?.Description
                        }
                    }
                },
            },
            Succeeded = true,
            Message = "Study Municipality created successfully.",
        };
    }
}
