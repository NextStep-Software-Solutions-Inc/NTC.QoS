using MediatR;
using NTC.QoS.Application.Features.MIMO.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Handlers.Queries;

public class GetAllMIMOsHandler(IRepository<Domain.Entities.MIMO> repository) : IRequestHandler<GetAllMIMOsRequest, PaginatedResponse<MIMODto>>
{
    private readonly IRepository<Domain.Entities.MIMO> _repository = repository;
    public async Task<PaginatedResponse<MIMODto>> Handle(GetAllMIMOsRequest request, CancellationToken cancellationToken)
    {
        var allMIMOs = await _repository.ListAsync(m => m.IsDeleted != true, cancellationToken);

        var mimo = allMIMOs.Select(m => new MIMODto
        {
            Id = m.Id,
            Type = m.Type,
            DLCoverageGain = m.DLCoverageGain,
            DLInterferenceReduction = m.DLInterferenceReduction,
            DLThroughput = m.DLThroughput,
            ULCoverageGain = m.ULCoverageGain,
            ULInterferenceReduction = m.ULInterferenceReduction,
            ULThroughput = m.ULThroughput,
        }).ToList();
        return new PaginatedResponse<MIMODto>(mimo, request.PageNumber, request.PageSize);
    }
}
