using MediatR;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;
using NTC.QoS.Domain.Models.DTOs.Study;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Queries;

public class GetAllStudyNetworksHandler(IStudyNetworkRepository repository) : IRequestHandler<GetAllStudyNetworksRequest, PaginatedResponse<StudyNetworkDto>>
{
    private readonly IStudyNetworkRepository _repository = repository;

    public async Task<PaginatedResponse<StudyNetworkDto>> Handle(GetAllStudyNetworksRequest request, CancellationToken cancellationToken)
    {
        var allStudyNetworks = await _repository.ListAsync(sn => !sn.IsDeleted, cancellationToken);
        if (allStudyNetworks is null)
        {
            return new PaginatedResponse<StudyNetworkDto>([], request.PageNumber, request.PageSize)
            {
                Succeeded = false,
                Message = "No records found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudyNetworks", ["No study networks found."]
                    }
                }
            };
        }

        if (request.StudyIds is not null && request.StudyIds.Count > 0)
        {
            allStudyNetworks = [.. allStudyNetworks.Where(sn => request.StudyIds.Contains(sn.StudyId))];
        }

        if (request.NetworkIds is not null && request.NetworkIds.Count > 0)
        {
            allStudyNetworks = [.. allStudyNetworks.Where(sn => request.NetworkIds.Contains(sn.NetworkId))];
        }

        var studyNetworkDtos = allStudyNetworks.Select(sn => new StudyNetworkDto
        {
            Id = sn.Id,
            Study = new StudyDto
            {
                Id = sn.StudyId,
                Name = sn.Study?.Name ?? string.Empty,
                Description = sn.Study?.Description ?? string.Empty,
                AreaStudy = new AreaStudyDto
                {
                    Id = sn.Study?.AreaStudy?.Id ?? Guid.Empty,
                    Name = sn.Study?.AreaStudy?.Name ?? string.Empty,
                    Description = sn.Study?.AreaStudy?.Description ?? string.Empty,
                },
                StudyRadius = sn.Study?.StudyRadius ?? 0
            },
            Network = new NetworkDto
            {
                Id = sn.NetworkId,
                Name = sn.Network?.Name ?? string.Empty,
                SystemTypeName = sn.Network?.SystemType.ToString() ?? string.Empty,
                NetworkTemplate = new NetworkTemplateDto
                {
                    Id = sn.Network?.NetworkTemplate?.Id ?? Guid.Empty,
                    Name = sn.Network?.NetworkTemplate?.Name ?? string.Empty,
                    Description = sn.Network?.NetworkTemplate?.Description ?? string.Empty,
                    URL = sn.Network?.NetworkTemplate?.URL ?? string.Empty
                },
                Uplink = new LinkNetworkDto
                {
                    Id = sn.Network?.UpLink?.Id ?? Guid.Empty,
                    Type = sn.Network?.UpLink?.Type.ToString() ?? string.Empty,
                    CenterFrequency = sn.Network?.UpLink?.CenterFrequency ?? 0
                },
                DownLink = new LinkNetworkDto
                {
                    Id = sn.Network?.DownLink?.Id ?? Guid.Empty,
                    Type = sn.Network?.DownLink?.Type.ToString() ?? string.Empty,
                    CenterFrequency = sn.Network?.DownLink?.CenterFrequency ?? 0
                }
            }
        }).ToList();

        return new PaginatedResponse<StudyNetworkDto>(studyNetworkDtos, request.PageNumber, request.PageSize)
        {
            Data = studyNetworkDtos,
            Succeeded = true,
            Message = "Study networks retrieved successfully."
        };
    }
}
