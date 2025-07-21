using MediatR;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

namespace NTC.QoS.Application.Features.StudyMunicipality.Handlers.Commands;

public class CreateStudyMunicipalityHandler(IStudyMunicipalityRepository repository, IStudyRepository studyRepository, IMunicipalityRepository municipalityRepository) : IRequestHandler<CreateStudyMunicipalityRequest, ApiResponse<StudyMunicipalityDto>>
{
    private readonly IStudyMunicipalityRepository _repository = repository;
    private readonly IStudyRepository _studyRepository = studyRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<ApiResponse<StudyMunicipalityDto>> Handle(CreateStudyMunicipalityRequest request, CancellationToken cancellationToken)
    {
        var studyMunicipalityQuery = _repository.Query(sm => sm.StudyId == request.StudyMunicipality.StudyId && sm.MunicipalityId == request.StudyMunicipality.MunicipalityId);
        var studyMunicipality = await _repository.FirstOrDefaultAsync(studyMunicipalityQuery, cancellationToken);

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

        var studyQuery = _studyRepository.Query(s => s.Id == request.StudyMunicipality.StudyId);
        var study = await _studyRepository.FirstOrDefaultAsync(studyQuery, cancellationToken);

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

        var municipalityQuery = _municipalityRepository.Query(s => s.Id == request.StudyMunicipality.MunicipalityId);
        var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

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
            StudyId = study.Id,
            MunicipalityId = municipality.Id,
        };

        _repository.Add(newStudyMunicipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<StudyMunicipalityDto>
        {
            Data = new StudyMunicipalityDto
            {
                Id = newStudyMunicipality.Id,
                Study = new()
                {
                    Id = newStudyMunicipality.StudyId,
                    Name = newStudyMunicipality.Study?.Name ?? string.Empty,
                    Description = newStudyMunicipality.Study?.Description,
                    StudyRadius = newStudyMunicipality.Study?.StudyRadius ?? 0,
                    AreaStudy = new()
                    {
                        Id = newStudyMunicipality.Study?.AreaStudyId ?? Guid.Empty,
                        Name = newStudyMunicipality.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = newStudyMunicipality.Study?.Description ?? string.Empty
                    }
                },
                Municipality = new()
                {
                    Id = newStudyMunicipality.MunicipalityId,
                    Name = newStudyMunicipality.Municipality?.Name ?? string.Empty,
                    IsCity = newStudyMunicipality.Municipality?.IsCity ?? false,
                    Description = newStudyMunicipality.Municipality?.Description,
                    Province = new()
                    {
                        Id = newStudyMunicipality.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = newStudyMunicipality.Municipality?.Province?.Name ?? string.Empty,
                        Description = newStudyMunicipality.Municipality?.Province?.Description,
                        Region = new()
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
