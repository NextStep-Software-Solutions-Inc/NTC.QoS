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
                ServiceProvider = new()
                {
                    Id = sn.Network != null && sn.Network.ServiceProvider != null ? sn.Network.ServiceProvider.Id : Guid.Empty,
                    MNC = sn.Network != null && sn.Network.ServiceProvider != null ? sn.Network.ServiceProvider.MNC : string.Empty,
                    MCC = sn.Network != null && sn.Network.ServiceProvider != null ? sn.Network.ServiceProvider.MCC : string.Empty,
                    Provider = new()
                    {
                        Id = sn.Network != null && sn.Network.ServiceProvider != null ? sn.Network.ServiceProvider.ProviderId : Guid.Empty,
                        Name = sn.Network != null && sn.Network.ServiceProvider != null && sn.Network.ServiceProvider.Provider != null ? sn.Network.ServiceProvider.Provider.Name : string.Empty,
                        Description = sn.Network != null && sn.Network.ServiceProvider != null && sn.Network.ServiceProvider.Provider != null ? sn.Network.ServiceProvider.Provider.Description : null,
                        Color = sn.Network != null && sn.Network.ServiceProvider != null && sn.Network.ServiceProvider.Provider != null ? sn.Network.ServiceProvider.Provider.Color : string.Empty
                    },
                    Service = new()
                    {
                        Id = sn.Network != null && sn.Network.ServiceProvider != null ? sn.Network.ServiceProvider.ServiceId : Guid.Empty,
                        Name = sn.Network != null && sn.Network.ServiceProvider != null && sn.Network.ServiceProvider.Service != null ? sn.Network.ServiceProvider.Service.Name : string.Empty,
                        Type = sn.Network != null && sn.Network.ServiceProvider != null && sn.Network.ServiceProvider.Service != null ? sn.Network.ServiceProvider.Service.Type.ToString() : string.Empty
                    }
                },
                Band = new()
                {
                    Id = sn.Network != null && sn.Network.Band != null ? sn.Network.Band.Id : Guid.Empty,
                    Name = sn.Network != null && sn.Network.Band != null ? sn.Network.Band.Name : string.Empty,
                    Description = sn.Network != null && sn.Network.Band != null ? sn.Network.Band.Description : null
                },
                Municipality = sn.Network != null && sn.Network.Municipality != null ? new()
                {
                    Id = sn.Network.Municipality.Id,
                    Name = sn.Network.Municipality.Name,
                    Description = sn.Network.Municipality.Description,
                    IsCity = sn.Network.Municipality.IsCity,
                    Code = sn.Network.Municipality.Code,
                    Area = sn.Network.Municipality.Area
                } : null,
                ULCenterFreq = sn.Network != null ? sn.Network.ULCenterFreq : 0,
                DLCenterFreq = sn.Network != null ? sn.Network.DLCenterFreq : 0,
                Bandwidth = sn.Network != null ? sn.Network.Bandwidth : 0
            }
        });
        return await _repository.PaginateAsync(studyNetworkDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
