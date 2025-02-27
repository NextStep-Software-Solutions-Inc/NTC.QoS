using MediatR;
using NTC.QoS.Application.Features.MIMO.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.MIMO.Handlers.Commands;

public class UpdateMIMOByIdHandler(IRepository<Domain.Entities.MIMO> repository) : IRequestHandler<UpdateMIMOByIdRequest, ApiResponse<Domain.Entities.MIMO>>
{
    private readonly IRepository<Domain.Entities.MIMO> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.MIMO>> Handle(UpdateMIMOByIdRequest request,
            CancellationToken cancellationToken)
    {
        var mimo = await _repository.FindAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);


        if (string.IsNullOrEmpty(request.MIMO.Type))
        {
            return new ApiResponse<Domain.Entities.MIMO>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Name is required." },
                Message = request.MIMO.Type + " already exists in our system. Please try a different one."
            };
        }
        if (mimo == null)
        {
            return new ApiResponse<Domain.Entities.MIMO>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "MIMO not found." },
                Message = "The mimo you are trying to edit does not exist."
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
        return new ApiResponse<Domain.Entities.MIMO>
        {
            Succeeded = true,
            Error = new[] { "" },
            Data = mimo,
            Message = "Request successful! Your changes have been saved.."
        };
    }
}