using MediatR;
using NTC.QoS.Application.Features.FrequencyAllocation.Handlers.Queries;
using NTC.QoS.Application.Features.FrequencyAllocation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Handlers.Commands;

public class UpdateFrequencyAllocationByIdHandler(
    IFrequencyAllocationRepository repository,
    ILinkRepository linkRepository)
    : IRequestHandler<UpdateFrequencyAllocationByIdRequest, ApiResponse<FrequencyAllocationDto>>
{
    private readonly IFrequencyAllocationRepository _repository = repository;
    private readonly ILinkRepository _linkRepository = linkRepository;

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
            var ul = await _linkRepository.FirstOrDefaultAsync(
                _linkRepository.Query(l => l.Id == dto.UpLinkId.Value), cancellationToken);
            if (ul is null)
                return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "UpLink not found." };
            entity.UpLinkId = dto.UpLinkId.Value;
        }

        if (dto.DownLinkId.HasValue)
        {
            var dl = await _linkRepository.FirstOrDefaultAsync(
                _linkRepository.Query(l => l.Id == dto.DownLinkId.Value), cancellationToken);
            if (dl is null)
                return new ApiResponse<FrequencyAllocationDto> { Succeeded = false, Message = "DownLink not found." };
            entity.DownLinkId = dto.DownLinkId.Value;
        }

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return await new GetFrequencyAllocationByIdHandler(_repository)
            .Handle(new GetFrequencyAllocationByIdRequest(entity.Id), cancellationToken);
    }
}
