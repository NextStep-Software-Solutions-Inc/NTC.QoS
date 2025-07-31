using MediatR;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Commands;

public class CreateStudyNetworkHandler(IStudyNetworkRepository repository, INetworkRepository networkRepository, IStudyRepository studyRepository) : IRequestHandler<CreateStudyNetworkRequest, ApiResponse<StudyNetworkDto>>
{
    private readonly IStudyNetworkRepository _repository = repository;
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IStudyRepository _studyRepository = studyRepository;

    public async Task<ApiResponse<StudyNetworkDto>> Handle(CreateStudyNetworkRequest request, CancellationToken cancellationToken)
    {
        var studyNetworkQuery = _repository.Query(sn => sn.StudyId == request.StudyNetwork.StudyId && sn.NetworkId == request.StudyNetwork.NetworkId);
        var studyNetwork = await _repository.FirstOrDefaultAsync(studyNetworkQuery, cancellationToken);
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

        var networkQuery = _networkRepository.Query(n => n.Id == request.StudyNetwork.NetworkId);
        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);
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

        var studyQuery = _studyRepository.Query(s => s.Id == request.StudyNetwork.StudyId);
        var study = await _studyRepository.FirstOrDefaultAsync(studyQuery, cancellationToken);
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
                Study = new()
                {
                    Id = newStudyNetwork.StudyId,
                    Name = newStudyNetwork.Study?.Name ?? string.Empty,
                    Description = newStudyNetwork.Study?.Description ?? string.Empty,
                    AreaStudy = new()
                    {
                        Id = newStudyNetwork.Study?.AreaStudy?.Id ?? Guid.Empty,
                        Name = newStudyNetwork.Study?.AreaStudy?.Name ?? string.Empty,
                        Description = newStudyNetwork.Study?.AreaStudy?.Description ?? string.Empty,
                    },
                    StudyRadius = newStudyNetwork.Study?.StudyRadius ?? 0
                },
                Network = new()
                {
                    Id = newStudyNetwork.NetworkId,
                    Name = newStudyNetwork.Network?.Name ?? string.Empty,
                    SystemTypeName = newStudyNetwork.Network?.SystemType?.Name ?? string.Empty,
                    NetworkTemplate = new()
                    {
                        Id = newStudyNetwork.Network?.NetworkTemplate?.Id ?? Guid.Empty,
                        Name = newStudyNetwork.Network?.NetworkTemplate?.Name ?? string.Empty,
                        Description = newStudyNetwork.Network?.NetworkTemplate?.Description ?? string.Empty,
                        URL = newStudyNetwork.Network?.NetworkTemplate?.URL ?? string.Empty
                    },
                    ServiceProvider = new()
                    {
                        Id = newStudyNetwork.Network?.ServiceProvider?.Id ?? Guid.Empty,
                        MNC = newStudyNetwork.Network?.ServiceProvider?.MNC ?? string.Empty,
                        MCC = newStudyNetwork.Network?.ServiceProvider?.MCC ?? string.Empty,
                        Provider = new()
                        {
                            Id = newStudyNetwork.Network?.ServiceProvider?.ProviderId ?? Guid.Empty,
                            Name = newStudyNetwork.Network?.ServiceProvider?.Provider?.Name ?? string.Empty,
                            Description = newStudyNetwork.Network?.ServiceProvider?.Provider?.Description,
                            Color = newStudyNetwork.Network?.ServiceProvider?.Provider?.Color ?? string.Empty
                        },
                        Service = new()
                        {
                            Id = newStudyNetwork.Network?.ServiceProvider?.ServiceId ?? Guid.Empty,
                            Name = newStudyNetwork.Network?.ServiceProvider?.Service?.Name ?? string.Empty,
                            Type = newStudyNetwork.Network?.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                        }
                    },
                    UpLink = new()
                    {
                        Id = newStudyNetwork.Network?.UpLink?.Id ?? Guid.Empty,
                        Type = newStudyNetwork.Network?.UpLink?.Type.ToString() ?? string.Empty,
                        CenterFrequency = newStudyNetwork.Network?.UpLink?.CenterFrequency ?? 0
                    },
                    DownLink = new()
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
