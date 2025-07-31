using MediatR;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Queries;

public class GetStudyNetworkByIdHandler(IStudyNetworkRepository repository) : IRequestHandler<GetStudyNetworkByIdRequest, ApiResponse<StudyNetworkDto>>
{
    private readonly IStudyNetworkRepository _repository = repository;

    public async Task<ApiResponse<StudyNetworkDto>> Handle(GetStudyNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var studyNetworkQuery = _repository.Query(sn => sn.Id == request.Id);
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
                Study = new()
                {
                    Id = studyNetwork.StudyId,
                    Name = studyNetwork.Study?.Name ?? string.Empty,
                    Description = studyNetwork.Study?.Description ?? string.Empty,
                    AreaStudy = new()
                    {
                        Id = studyNetwork.Study?.AreaStudy?.Id ?? Guid.Empty,
                        Name = studyNetwork.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = studyNetwork.Study?.AreaStudy?.Description ?? string.Empty,
                    },
                    StudyRadius = studyNetwork.Study?.StudyRadius ?? 0
                },
                Network = new()
                {
                    Id = studyNetwork.NetworkId,
                    Name = studyNetwork.Network?.Name ?? string.Empty,
                    SystemTypeName = studyNetwork.Network?.SystemType?.Name ?? string.Empty,
                    NetworkTemplate = new()
                    {
                        Id = studyNetwork.Network?.NetworkTemplate?.Id ?? Guid.Empty,
                        Name = studyNetwork.Network?.NetworkTemplate?.Name ?? string.Empty,
                        Description = studyNetwork.Network?.NetworkTemplate?.Description ?? string.Empty,
                        URL = studyNetwork.Network?.NetworkTemplate?.URL ?? string.Empty
                    },
                    ServiceProvider = new()
                    {
                        Id = studyNetwork.Network?.ServiceProvider?.Id ?? Guid.Empty,
                        MNC = studyNetwork.Network?.ServiceProvider?.MNC ?? string.Empty,
                        MCC = studyNetwork.Network?.ServiceProvider?.MCC ?? string.Empty,
                        Provider = new()
                        {
                            Id = studyNetwork.Network?.ServiceProvider?.ProviderId ?? Guid.Empty,
                            Name = studyNetwork.Network?.ServiceProvider?.Provider?.Name ?? string.Empty,
                            Description = studyNetwork.Network?.ServiceProvider?.Provider?.Description,
                            Color = studyNetwork.Network?.ServiceProvider?.Provider?.Color ?? string.Empty
                        },
                        Service = new()
                        {
                            Id = studyNetwork.Network?.ServiceProvider?.ServiceId ?? Guid.Empty,
                            Name = studyNetwork.Network?.ServiceProvider?.Service?.Name ?? string.Empty,
                            Type = studyNetwork.Network?.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                        }
                    },
                    UpLink = new()
                    {
                        Id = studyNetwork.Network?.UpLink?.Id ?? Guid.Empty,
                        Type = studyNetwork.Network?.UpLink?.Type.ToString() ?? string.Empty,
                        CenterFrequency = studyNetwork.Network?.UpLink?.CenterFrequency ?? 0
                    },
                    DownLink = new()
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