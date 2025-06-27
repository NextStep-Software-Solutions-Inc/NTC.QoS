using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Queries;

public class GetStudyNetworkrByIdHandler(IStudyNetworkRepository repository) : IRequestHandler<GetStudyNetworkByIdRequest, ApiResponse<StudyNetworkDto>>
{
    private readonly IStudyNetworkRepository _repository = repository;

    public async Task<ApiResponse<StudyNetworkDto>> Handle(GetStudyNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var studyNetworkQuery = _repository.Query(sn => sn.Id == request.Id && !sn.IsDeleted);
        var studyNetwork = await _repository.FirstOrDefaultAsync(studyNetworkQuery, cancellationToken);
        if (studyNetwork is null)
        {
            return new ApiResponse<StudyNetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudyNetwork", [ "Study Network not found."]
                    }
                },
                Message = "The study network does not exist."
            };
        }

        return new ApiResponse<StudyNetworkDto>
        {
            Succeeded = true,
            Message = "Study Network retrieved successfully.",
            Data = new StudyNetworkDto()
            {
                Id = studyNetwork.Id,
                Study = new StudyDto
                {
                    Id = studyNetwork.StudyId,
                    Name = studyNetwork.Study?.Name ?? string.Empty,
                    Description = studyNetwork.Study?.Description ?? string.Empty,
                    AreaStudy = new AreaStudyDto
                    {
                        Id = studyNetwork.Study?.AreaStudy?.Id ?? Guid.Empty,
                        Name = studyNetwork.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = studyNetwork.Study?.AreaStudy?.Description ?? string.Empty,
                    },
                    StudyRadius = studyNetwork.Study?.StudyRadius ?? 0
                },
                Network = new NetworkDto
                {
                    Id = studyNetwork.NetworkId,
                    Name = studyNetwork.Network?.Name ?? string.Empty,
                    SystemTypeName = studyNetwork.Network?.SystemType?.Name ?? string.Empty,
                    NetworkTemplate = new NetworkTemplateDto
                    {
                        Id = studyNetwork.Network?.NetworkTemplate?.Id ?? Guid.Empty,
                        Name = studyNetwork.Network?.NetworkTemplate?.Name ?? string.Empty,
                        Description = studyNetwork.Network?.NetworkTemplate?.Description ?? string.Empty,
                        URL = studyNetwork.Network?.NetworkTemplate?.URL ?? string.Empty
                    },
                    Uplink = new LinkNetworkDto
                    {
                        Id = studyNetwork.Network?.UpLink?.Id ?? Guid.Empty,
                        Type = studyNetwork.Network?.UpLink?.Type.ToString() ?? string.Empty,
                        CenterFrequency = studyNetwork.Network?.UpLink?.CenterFrequency ?? 0
                    },
                    DownLink = new LinkNetworkDto
                    {
                        Id = studyNetwork.Network?.DownLink?.Id ?? Guid.Empty,
                        Type = studyNetwork.Network?.DownLink?.Type.ToString() ?? string.Empty,
                        CenterFrequency = studyNetwork.Network?.DownLink?.CenterFrequency ?? 0
                    }
                }
            }
        };
    }
}
