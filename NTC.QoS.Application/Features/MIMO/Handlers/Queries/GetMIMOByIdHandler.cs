using MediatR;
using NTC.QoS.Application.Features.MIMO.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Handlers.Queries;

public class GetMIMOByIdHandler(IRepository<Domain.Entities.MIMO> repository) : IRequestHandler<GetMIMOByIdRequest, ApiResponse<MIMODto>>
{
    private readonly IRepository<Domain.Entities.MIMO> _repository = repository;
    public async Task<ApiResponse<MIMODto>> Handle(GetMIMOByIdRequest request, CancellationToken cancellationToken)
    {
        var mimo = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (mimo == null)
        {
            return new ApiResponse<MIMODto>
            {
                Succeeded = false,
                Message = "MIMO not found.",
                Error = new[] { "Invalid MIMO does not exist." }
            };
        }
        return new ApiResponse<MIMODto>
        {
            Succeeded = true,
            Message = "MIMO retrieved successfully",
            Data = new MIMODto
            {
                Id = mimo.Id,
                Type = mimo.Type,
                DLCoverageGain = mimo.DLCoverageGain,
                DLInterferenceReduction = mimo.DLInterferenceReduction,
                DLThroughput = mimo.DLThroughput,
                ULCoverageGain = mimo.ULCoverageGain,
                ULInterferenceReduction = mimo.ULInterferenceReduction,
                ULThroughput = mimo.ULThroughput,
            }
        };
    }
}
