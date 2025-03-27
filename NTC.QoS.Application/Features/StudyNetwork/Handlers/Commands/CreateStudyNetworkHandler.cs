using MediatR;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.Study;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Commands;

public class CreateStudyNetworkHandler(IRepository<Domain.Aggregates.StudyNetwork> repository) : IRequestHandler<CreateStudyNetworkRequest, ApiResponse<StudyNetworkDto>>
{
    private readonly IRepository<Domain.Aggregates.StudyNetwork> _repository = repository;

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
                    {"Error", ["Study network with the same study and network already exists."] }
                },
            };
        }

        var newStudyNetwork = new Domain.Aggregates.StudyNetwork 
        {
            NetworkId = request.StudyNetwork.NetworkId,
            StudyId = request.StudyNetwork.StudyId 
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
                    SystemType = newStudyNetwork.Network?.SystemType.ToString() ?? string.Empty
                }
            },
            Message = "Study network created successfully.",
            Succeeded = true
        };
    }
}
