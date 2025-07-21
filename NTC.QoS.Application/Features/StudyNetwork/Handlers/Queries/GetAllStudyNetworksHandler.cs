using MediatR;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Queries;

public class GetAllStudyNetworksHandler(IStudyNetworkRepository repository) : IRequestHandler<GetAllStudyNetworksRequest, PaginatedResponse<StudyNetworkDto>>
{
    private readonly IStudyNetworkRepository _repository = repository;

    public async Task<PaginatedResponse<StudyNetworkDto>> Handle(GetAllStudyNetworksRequest request, CancellationToken cancellationToken)
    {
        var allStudyNetworks = _repository.Query();
        if (allStudyNetworks is null)
        {
            return new PaginatedResponse<StudyNetworkDto>([], request.PageNumber, request.PageSize, 0)
            {
                Succeeded = false,
                Message = "No records found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudyNetworks", new[] { "No study networks found." }
                    }
                }
            };
        }

        if (request.StudyIds is not null && request.StudyIds.Count > 0)
        {
            allStudyNetworks = allStudyNetworks.Where(sn => request.StudyIds.Contains(sn.StudyId));
        }

        if (request.NetworkIds is not null && request.NetworkIds.Count > 0)
        {
            allStudyNetworks = allStudyNetworks.Where(sn => request.NetworkIds.Contains(sn.NetworkId));
        }

        var studyNetworkDtos = allStudyNetworks.Select(sn => new StudyNetworkDto
        {
            Id = sn.Id,
            Study = new()
            {
                Id = sn.StudyId,
                Name = sn.Study != null ? sn.Study.Name : string.Empty,
                Description = sn.Study != null ? sn.Study.Description : string.Empty,
                AreaStudy = new()
                {
                    Id = sn.Study != null && sn.Study.AreaStudy != null ? sn.Study.AreaStudy.Id : Guid.Empty,
                    Name = sn.Study != null && sn.Study.AreaStudy != null ? sn.Study.AreaStudy.Name : string.Empty,
                    Description = sn.Study != null && sn.Study.AreaStudy != null ? sn.Study.AreaStudy.Description : string.Empty,
                },
                StudyRadius = sn.Study != null ? sn.Study.StudyRadius : 0
            },
            Network = new()
            {
                Id = sn.NetworkId,
                Name = sn.Network != null ? sn.Network.Name : string.Empty,
                SystemTypeName = sn.Network != null && sn.Network.SystemType != null ? sn.Network.SystemType.Name : string.Empty,
                NetworkTemplate = new()
                {
                    Id = sn.Network != null && sn.Network.NetworkTemplate != null ? sn.Network.NetworkTemplate.Id : Guid.Empty,
                    Name = sn.Network != null && sn.Network.NetworkTemplate != null ? sn.Network.NetworkTemplate.Name : string.Empty,
                    Description = sn.Network != null && sn.Network.NetworkTemplate != null ? sn.Network.NetworkTemplate.Description : string.Empty,
                    URL = sn.Network != null && sn.Network.NetworkTemplate != null ? sn.Network.NetworkTemplate.URL : string.Empty
                },
                UpLink = new()
                {
                    Id = sn.Network != null && sn.Network.UpLink != null ? sn.Network.UpLink.Id : Guid.Empty,
                    Type = sn.Network != null && sn.Network.UpLink != null ? sn.Network.UpLink.Type.ToString() : string.Empty,
                    CenterFrequency = sn.Network != null && sn.Network.UpLink != null ? sn.Network.UpLink.CenterFrequency : 0
                },
                DownLink = new()
                {
                    Id = sn.Network != null && sn.Network.DownLink != null ? sn.Network.DownLink.Id : Guid.Empty,
                    Type = sn.Network != null && sn.Network.DownLink != null ? sn.Network.DownLink.Type.ToString() : string.Empty,
                    CenterFrequency = sn.Network != null && sn.Network.DownLink != null ? sn.Network.DownLink.CenterFrequency : 0
                }
            }
        });

        return await _repository.PaginateAsync(studyNetworkDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
