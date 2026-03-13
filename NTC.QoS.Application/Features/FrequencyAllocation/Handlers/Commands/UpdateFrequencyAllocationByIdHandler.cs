using MediatR;
using NTC.QoS.Application.Features.FrequencyAllocation.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Handlers.Commands;

public class UpdateFrequencyAllocationByIdHandler(
    IFrequencyAllocationRepository repository,
    ILinkRepository linkRepository,
    IMediator mediator)
    : IRequestHandler<UpdateFrequencyAllocationByIdRequest, ApiResponse<FrequencyAllocationDto>>
{
    private readonly IFrequencyAllocationRepository _repository = repository;
    private readonly ILinkRepository _linkRepository = linkRepository;
    private readonly IMediator _mediator = mediator;

    public async Task<ApiResponse<FrequencyAllocationDto>> Handle(
        UpdateFrequencyAllocationByIdRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.FirstOrDefaultAsync(
            _repository.Query(fa => fa.Id == request.Id), cancellationToken);

        if (entity is null)
            return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "Frequency allocation not found." };

        var dto = request.FrequencyAllocation;

        if (!string.IsNullOrEmpty(dto.Name))  entity.Name     = dto.Name;
        if (dto.Notes    is not null)          entity.Notes    = dto.Notes;
        if (dto.IsActive is not null)          entity.IsActive = dto.IsActive.Value;

        if (dto.UpLinkId.HasValue)
        {
            var valid = await _linkRepository.ValidateLinkTypesAsync(dto.UpLinkId.Value, LinkTypeEnum.Uplink, cancellationToken);
            if (!valid)
                return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "UpLink not found or is not of type Uplink." };
            entity.UpLinkId = dto.UpLinkId.Value;
        }

        if (dto.DownLinkId.HasValue)
        {
            var valid = await _linkRepository.ValidateLinkTypesAsync(dto.DownLinkId.Value, LinkTypeEnum.Downlink, cancellationToken);
            if (!valid)
                return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "DownLink not found or is not of type Downlink." };
            entity.DownLinkId = dto.DownLinkId.Value;
        }

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetFrequencyAllocationByIdRequest(entity.Id), cancellationToken);
    }
}
