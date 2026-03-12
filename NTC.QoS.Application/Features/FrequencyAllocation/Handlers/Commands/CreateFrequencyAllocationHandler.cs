using MediatR;
using NTC.QoS.Application.Features.FrequencyAllocation.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Handlers.Commands;

public class CreateFrequencyAllocationHandler(
    IFrequencyAllocationRepository repository,
    IProviderRepository providerRepository,
    ILinkRepository linkRepository,
    IMediator mediator)
    : IRequestHandler<CreateFrequencyAllocationRequest, ApiResponse<FrequencyAllocationDto>>
{
    private readonly IFrequencyAllocationRepository _repository = repository;
    private readonly IProviderRepository _providerRepository = providerRepository;
    private readonly ILinkRepository _linkRepository = linkRepository;
    private readonly IMediator _mediator = mediator;

    public async Task<ApiResponse<FrequencyAllocationDto>> Handle(
        CreateFrequencyAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var dto = request.FrequencyAllocation;

        var duplicate = await _repository.FirstOrDefaultAsync(
            _repository.Query(fa =>
                fa.ProviderId == dto.ProviderId &&
                fa.UpLinkId   == dto.UpLinkId &&
                fa.DownLinkId == dto.DownLinkId),
            cancellationToken);

        if (duplicate is not null)
            return new ApiResponse<FrequencyAllocationDto>
            {
                Succeeded = false,
                Message   = "A frequency allocation with the same Provider and Links already exists.",
                Errors    = new Dictionary<string, string[]> { { "Duplicate", ["Duplicate frequency allocation."] } }
            };

        var provider = await _providerRepository.FirstOrDefaultAsync(
            _providerRepository.Query(p => p.Id == dto.ProviderId), cancellationToken);
        if (provider is null)
            return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "Provider not found." };

        var upLinkValid = await _linkRepository.ValidateLinkTypesAsync(dto.UpLinkId, LinkTypeEnum.Uplink, cancellationToken);
        if (!upLinkValid)
            return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "UpLink not found or is not of type Uplink." };

        var downLinkValid = await _linkRepository.ValidateLinkTypesAsync(dto.DownLinkId, LinkTypeEnum.Downlink, cancellationToken);
        if (!downLinkValid)
            return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "DownLink not found or is not of type Downlink." };

        var entity = new Domain.Entities.FrequencyAllocation
        {
            Name       = dto.Name,
            Notes      = dto.Notes,
            ProviderId = dto.ProviderId,
            UpLinkId   = dto.UpLinkId,
            DownLinkId = dto.DownLinkId,
            IsActive   = true,
        };

        _repository.Add(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetFrequencyAllocationByIdRequest(entity.Id), cancellationToken);
    }
}
