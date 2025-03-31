using MediatR;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Entities;
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
        var allStudyMunicipalities = await _studyMunicipalityRepository.ListAsync(sm => !sm.IsDeleted, cancellationToken);

        allStudyMunicipalities = allStudyMunicipalities
            .Where(sm => !request.MunicipalityId.HasValue || sm.MunicipalityId == request.MunicipalityId.Value)
            .Where(sm => !request.StudyId.HasValue || sm.StudyId == request.StudyId.Value)
            .ToList();

        var studyMunicipalityDtos = allStudyMunicipalities.Select(studyMunicipality => new StudyMunicipalityDto
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
        }).ToList();

        if (studyMunicipalityDtos.Count is 0)
        {
            return new PaginatedResponse<StudyMunicipalityDto>(studyMunicipalityDtos, request.PageNumber, request.PageSize)
            {
                Data = studyMunicipalityDtos,
                Message = "No records found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudyMunicipalities", ["No study municipalities found."]
                    }
                }
            };
        }

        return new PaginatedResponse<StudyMunicipalityDto>(studyMunicipalityDtos, request.PageNumber, request.PageSize)
        {
            Data = studyMunicipalityDtos,
            Succeeded = true,
            Message = "Study Municipalities retrieved successfully."
        };
    }
}
