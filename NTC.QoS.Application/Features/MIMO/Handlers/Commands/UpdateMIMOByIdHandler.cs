using MediatR;
using NTC.QoS.Application.Features.MIMO.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Handlers.Commands;

public class UpdateMIMOByIdHandler(IRepository<Domain.Entities.MIMO> repository) : IRequestHandler<UpdateMIMOByIdRequest, ApiResponse<MIMODto>>
{
    private readonly IRepository<Domain.Entities.MIMO> _repository = repository;
    public async Task<ApiResponse<MIMODto>> Handle(UpdateMIMOByIdRequest request,
            CancellationToken cancellationToken)
    {
        var mimoQuery = _repository.Query(m => m.Id == request.Id);
        var mimo = await _repository.FirstOrDefaultAsync(mimoQuery, cancellationToken);

        if (string.IsNullOrEmpty(request.MIMO.Type))
        {
            return new ApiResponse<MIMODto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Type", new[] { "MIMO type is required." } }
                },
                Message = "MIMO type is required."
            };
        }
        if (mimo is null)
        {
            return new ApiResponse<MIMODto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                     { "Id", new[] { "MIMO not found." } }
                },
                Message = "The MIMO you are trying to edit does not exist."
            };
        }
        mimo.Type = request.MIMO.Type;
        mimo.DLThroughput = request.MIMO.DLThroughput;
        mimo.ULThroughput = request.MIMO.ULThroughput;
        mimo.DLInterferenceReduction = request.MIMO.DLInterferenceReduction;
        mimo.ULInterferenceReduction = request.MIMO.ULInterferenceReduction;
        mimo.DLCoverageGain = request.MIMO.DLCoverageGain;
        mimo.ULCoverageGain = request.MIMO.ULCoverageGain;

        _repository.Update(mimo);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<MIMODto>
        {
            Succeeded = true,
            Data = new MIMODto
            {
                Id = mimo.Id,
                Type = mimo.Type,
                DLCoverageGain = mimo.DLCoverageGain,
                DLInterferenceReduction = mimo.DLInterferenceReduction,
                DLThroughput = mimo.DLThroughput,
                ULCoverageGain = mimo.ULCoverageGain,
                ULInterferenceReduction = mimo.ULInterferenceReduction,
                ULThroughput = mimo.ULThroughput
            },
            Message = "MIMO updated successfully."
        };
    }
}