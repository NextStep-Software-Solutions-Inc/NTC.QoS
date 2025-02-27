using MediatR;
using NTC.QoS.Application.Features.MIMO.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Handlers.Commands;

public class CreateMIMOHandler(IRepository<Domain.Entities.MIMO> repository) : IRequestHandler<CreateMIMORequest, ApiResponse<CreateMIMODto>>
{
    private readonly IRepository<Domain.Entities.MIMO> _repository = repository;

    public async Task<ApiResponse<CreateMIMODto>> Handle(CreateMIMORequest request, CancellationToken cancellationToken)
    {
        var mimo = await _repository.FindAsync(m => m.Type == request.MIMO.Type && !m.IsDeleted, cancellationToken);

        if (mimo != null)
        {
            return new ApiResponse<CreateMIMODto>
            {
                Message = "MIMO already exists.",
                Succeeded = false
            };
        }

        var newMIMO = new Domain.Entities.MIMO
        {
            Type = request.MIMO.Type,
            DLCoverageGain = request.MIMO.DLCoverageGain,
            ULCoverageGain = request.MIMO.ULCoverageGain,
            DLInterferenceReduction = request.MIMO.DLInterferenceReduction,
            ULInterferenceReduction = request.MIMO.ULInterferenceReduction,
            DLThroughput = request.MIMO.DLThroughput,
            ULThroughput = request.MIMO.ULThroughput,
        };

        _repository.Add(newMIMO);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateMIMODto>
        {
            Data = new CreateMIMODto
            {
                Type = newMIMO.Type,
                DLCoverageGain = newMIMO.DLCoverageGain,
                ULCoverageGain = newMIMO.ULCoverageGain,
                DLInterferenceReduction = newMIMO.DLInterferenceReduction,
                ULInterferenceReduction = newMIMO.ULInterferenceReduction,
                DLThroughput = newMIMO.DLThroughput,
                ULThroughput = newMIMO.ULThroughput,
            },
            Message = "MIMO created successfully.",
            Succeeded = true
        };
    }
}
