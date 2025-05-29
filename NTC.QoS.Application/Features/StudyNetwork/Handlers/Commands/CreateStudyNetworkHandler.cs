using MediatR;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;
using NTC.QoS.Domain.Models.DTOs.Study;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Commands;

public class CreateStudyNetworkHandler(IStudyNetworkRepository repository, INetworkRepository networkRepository, IStudyRepository studyRepository) : IRequestHandler<CreateStudyNetworkRequest, ApiResponse<StudyNetworkDto>>
{
    private readonly IStudyNetworkRepository _repository = repository;
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IStudyRepository _studyRepository = studyRepository;

    public async Task<ApiResponse<StudyNetworkDto>> Handle(CreateStudyNetworkRequest request, CancellationToken cancellationToken)
    {
        var studyNetwork = await _repository.FindAsync(sn => sn.StudyId == request.StudyNetwork.StudyId && sn.NetworkId == request.StudyNetwork.NetworkId, cancellationToken);
        if (studyNetwork is not null)
        {
            return new ApiResponse<StudyNetworkDto>
            {
                Message = "Study network already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Study network with the same study and network already exists."]
                    }
                }
            };
        }

        var network = await _networkRepository.GetByIdAsync(request.StudyNetwork.NetworkId, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<StudyNetworkDto>
            {
                Message = "Network not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullNetwork", ["Network not found."]
                    }
                }
            };
        }

        var study = await _studyRepository.GetByIdAsync(request.StudyNetwork.StudyId, cancellationToken);
        if (study is null)
        {
            return new ApiResponse<StudyNetworkDto>
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

        var newStudyNetwork = new Domain.Aggregates.StudyNetwork 
        {
            NetworkId = network.Id,
            StudyId = study.Id
        };

        _repository.Add(newStudyNetwork);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<StudyNetworkDto>
        {
            Data = new StudyNetworkDto
            {
                Id = newStudyNetwork.Id,
                Study = new StudyDto
                {
                    Id = newStudyNetwork.StudyId,
                    Name = newStudyNetwork.Study?.Name ?? string.Empty,
                    Description = newStudyNetwork.Study?.Description ?? string.Empty,
                    AreaStudy = new AreaStudyDto
                    {
                        Id = newStudyNetwork.Study?.AreaStudy?.Id ?? Guid.Empty,
                        Name = newStudyNetwork.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = newStudyNetwork.Study?.AreaStudy?.Description ?? string.Empty,
                    },
                    StudyRadius = newStudyNetwork.Study?.StudyRadius ?? 0
                },
                Network = new NetworkDto
                {
                    Id = newStudyNetwork.NetworkId,
                    Name = newStudyNetwork.Network?.Name ?? string.Empty,
                    SystemTypeName = newStudyNetwork.Network?.SystemType.ToString() ?? string.Empty,
                    NetworkTemplate = new NetworkTemplateDto
                    {
                        Id = newStudyNetwork.Network?.NetworkTemplate?.Id ?? Guid.Empty,
                        Name = newStudyNetwork.Network?.NetworkTemplate?.Name ?? string.Empty,
                        Description = newStudyNetwork.Network?.NetworkTemplate?.Description ?? string.Empty,
                        URL = newStudyNetwork.Network?.NetworkTemplate?.URL ?? string.Empty
                    },
                    Uplink = new LinkNetworkDto
                    {
                        Id = newStudyNetwork.Network?.UpLink?.Id ?? Guid.Empty,
                        Type = newStudyNetwork.Network?.UpLink?.Type.ToString() ?? string.Empty,
                        CenterFrequency = newStudyNetwork.Network?.UpLink?.CenterFrequency ?? 0
                    },
                    DownLink = new LinkNetworkDto
                    {
                        Id = newStudyNetwork.Network?.DownLink?.Id ?? Guid.Empty,
                        Type = newStudyNetwork.Network?.DownLink?.Type.ToString() ?? string.Empty,
                        CenterFrequency = newStudyNetwork.Network?.DownLink?.CenterFrequency ?? 0
                    }
                }
            },
            Message = "Study network created successfully.",
            Succeeded = true
        };
    }
}
